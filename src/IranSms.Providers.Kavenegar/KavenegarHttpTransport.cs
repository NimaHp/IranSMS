using System.Text;

namespace IranSms.Providers.Kavenegar
{
    /// <summary>
    /// Real Kavenegar transport backed by <see cref="HttpClient"/>.
    /// Posts form-urlencoded to https://api.kavenegar.com/v1/{api-key}/{method}.json.
    /// </summary>
    internal sealed class KavenegarHttpTransport : IKavenegarTransport, IDisposable
    {
        private const string BaseUrl = "https://api.kavenegar.com/v1";
        private const long MaxResponseBytes = 1_048_576;
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly bool _ownsHttp;

        /// <summary>Initializes a new instance of the <see cref="KavenegarHttpTransport"/> class.</summary>
        /// <param name="apiKey">Kavenegar API key.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/> — when supplied, its lifetime is caller-owned; otherwise the transport owns and disposes the internal client.</param>
        public KavenegarHttpTransport(string apiKey, HttpClient? httpClient = null)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _ownsHttp = httpClient is null;
            _http = httpClient ?? new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
            })
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }

        /// <inheritdoc />
        public async Task<string> PostAsync(
            string method,
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            var url = BuildUrl(method);
            using (var content = new FormUrlEncodedContent(parameters))
            {
                using (var response = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
                {
                    if (response.Content.Headers.ContentLength > MaxResponseBytes)
                        throw new IranSmsException("Kavenegar response exceeded the maximum allowed size.")
                        {
                            ProviderName = "Kavenegar",
                            Kind = SmsErrorKind.Transport,
                        };
                    var body = await ReadBodyAsync(response.Content, "Kavenegar").ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new IranSmsException(
                            $"Kavenegar HTTP error ({(int)response.StatusCode}).")
                        {
                            ProviderName = "Kavenegar",
                            ProviderStatusCode = (int)response.StatusCode,
                            Kind = SmsErrorKindExtensions.FromHttpStatus((int)response.StatusCode),
                            Operation = method,
                            RawResponseBody = body,
                        };
                    }

                    return body;
                }
            }
        }

        /// <inheritdoc />
        public async Task<string> GetAsync(string method, CancellationToken cancellationToken)
        {
            var url = BuildUrl(method);
            using (var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                if (response.Content.Headers.ContentLength > MaxResponseBytes)
                    throw new IranSmsException("Kavenegar response exceeded the maximum allowed size.")
                    {
                        ProviderName = "Kavenegar",
                        Kind = SmsErrorKind.Transport,
                    };
                var body = await ReadBodyAsync(response.Content, "Kavenegar").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new IranSmsException(
                        $"Kavenegar HTTP error ({(int)response.StatusCode}).")
                    {
                        ProviderName = "Kavenegar",
                        ProviderStatusCode = (int)response.StatusCode,
                        Kind = SmsErrorKindExtensions.FromHttpStatus((int)response.StatusCode),
                        Operation = method,
                        RawResponseBody = body,
                    };
                }

                return body;
            }
        }

        private string BuildUrl(string method)
        {
            var escapedKey = Uri.EscapeDataString(_apiKey);
            if (_http.BaseAddress is null)
                return $"{BaseUrl}/{escapedKey}/{method}.json";

            var baseUrl = _http.BaseAddress.ToString().TrimEnd('/');
            if (baseUrl.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
                return $"{baseUrl}/{escapedKey}/{method}.json";
            return $"{baseUrl}/v1/{escapedKey}/{method}.json";
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
