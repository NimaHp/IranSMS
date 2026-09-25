using System.Text;

namespace IranSms.Providers.Melipayamak
{
    /// <summary>
    /// Real Melipayamak transport backed by <see cref="HttpClient"/>.
    /// Posts form-urlencoded to https://rest.payamak-panel.com/api/SendSMS/{action}.
    /// </summary>
    internal sealed class MelipayamakHttpTransport : IMelipayamakTransport, IDisposable
    {
        private const string BaseUrl = "https://rest.payamak-panel.com/api/SendSMS";
        private const long MaxResponseBytes = 1_048_576;
        private readonly HttpClient _http;
        private readonly bool _ownsHttp;

        /// <summary>Initializes a new instance of the <see cref="MelipayamakHttpTransport"/> class.</summary>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/> — caller-owned when supplied; transport-owned otherwise.</param>
        public MelipayamakHttpTransport(HttpClient? httpClient = null)
        {
            _ownsHttp = httpClient is null;
            _http = httpClient ?? new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
            })
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
        }

        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }

        /// <inheritdoc />
        public async Task<string> PostFormAsync(
            string action,
            IReadOnlyDictionary<string, string> form,
            CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/{action}";
            using (var content = new FormUrlEncodedContent(form))
            {
                using (var response = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
                {
                    if (response.Content.Headers.ContentLength > MaxResponseBytes)
                        throw new IranSmsException("Melipayamak response exceeded the maximum allowed size.")
                        {
                            ProviderName = "Melipayamak",
                            Kind = SmsErrorKind.Transport,
                        };
                    var body = await ReadBodyAsync(response.Content, "Melipayamak").ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new IranSmsException(
                            $"Melipayamak HTTP error ({(int)response.StatusCode}).")
                        {
                            ProviderName = "Melipayamak",
                            ProviderStatusCode = (int)response.StatusCode,
                            Kind = SmsErrorKindExtensions.FromHttpStatus((int)response.StatusCode),
                            Operation = action,
                            RawResponseBody = body,
                        };
                    }

                    return body;
                }
            }
        }

        private static async Task<string> ReadBodyAsync(HttpContent content, string providerName)
        {
            if (content.Headers.ContentLength > MaxResponseBytes)
                throw new IranSmsException($"{providerName} response exceeded the maximum allowed size.")
                {
                    ProviderName = providerName,
                    Kind = SmsErrorKind.Transport,
                };

            using var stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
            using var memory = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                if (memory.Length + read > MaxResponseBytes)
                    throw new IranSmsException($"{providerName} response exceeded the maximum allowed size.")
                    {
                        ProviderName = providerName,
                        Kind = SmsErrorKind.Transport,
                    };
                await memory.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            }
            return Encoding.UTF8.GetString(memory.ToArray());
        }
    }
}
