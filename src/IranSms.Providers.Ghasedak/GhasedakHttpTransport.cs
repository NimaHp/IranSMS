using System.Text;

namespace IranSms.Providers.Ghasedak
{
    /// <summary>
    /// Real HTTP transport for Ghasedak: posts JSON bodies and issues GETs
    /// against the gateway with the ApiKey header.
    /// </summary>
    internal sealed class GhasedakHttpTransport : IGhasedakTransport, IDisposable
    {
        private const string BaseUrl = "https://gateway.ghasedak.me/rest/api/v1/WebService/";
        private const long MaxResponseBytes = 1_048_576;
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly bool _ownsHttp;

        public GhasedakHttpTransport(HttpClient? httpClient, string apiKey)
        {
            _ownsHttp = httpClient is null;
            _http = httpClient ?? new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
            })
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }

        public async Task<string> PostJsonAsync(string endpoint, string jsonBody, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + endpoint)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation("ApiKey", _apiKey);

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                throw new IranSmsException("Ghasedak response exceeded the maximum allowed size.")
                {
                    ProviderName = "Ghasedak",
                    Kind = SmsErrorKind.Transport,
                };
            var body = await ReadBodyAsync(response.Content, "Ghasedak").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new IranSmsException($"Ghasedak HTTP error ({(int)response.StatusCode}).")
                {
                    ProviderName = "Ghasedak",
                    ProviderStatusCode = (int)response.StatusCode,
                    Kind = SmsErrorKindExtensions.FromHttpStatus((int)response.StatusCode),
                    Operation = endpoint,
                    RawResponseBody = body,
                };
            }

            return body;
        }

        public async Task<string> GetAsync(string endpoint, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
        {
            var url = BaseUrl + endpoint;
            var sb = new StringBuilder(url);
            var first = true;
            foreach (var kv in query)
            {
                sb.Append(first ? '?' : '&');
                first = false;
                sb.Append(Uri.EscapeDataString(kv.Key)).Append('=').Append(Uri.EscapeDataString(kv.Value));
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, sb.ToString());
            request.Headers.TryAddWithoutValidation("ApiKey", _apiKey);

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.Content.Headers.ContentLength > MaxResponseBytes)
                throw new IranSmsException("Ghasedak response exceeded the maximum allowed size.")
                {
                    ProviderName = "Ghasedak",
                    Kind = SmsErrorKind.Transport,
                };
            var body = await ReadBodyAsync(response.Content, "Ghasedak").ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new IranSmsException($"Ghasedak HTTP error ({(int)response.StatusCode}).")
                {
                    ProviderName = "Ghasedak",
                    ProviderStatusCode = (int)response.StatusCode,
                    Kind = SmsErrorKindExtensions.FromHttpStatus((int)response.StatusCode),
                    Operation = endpoint,
                    RawResponseBody = body,
                };
            }

            return body;
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
