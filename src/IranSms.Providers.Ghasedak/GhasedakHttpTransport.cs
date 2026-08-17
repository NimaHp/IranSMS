using System.Text;

namespace IranSms.Providers.Ghasedak
{
    /// <summary>
    /// Real HTTP transport for Ghasedak: posts JSON bodies and issues GETs
    /// against the gateway with the ApiKey header.
    /// </summary>
    internal sealed class GhasedakHttpTransport : IGhasedakTransport
    {
        private const string BaseUrl = "https://gateway.ghasedak.me/rest/api/v1/WebService/";
        private readonly HttpClient _http;
        private readonly string _apiKey;

        public GhasedakHttpTransport(HttpClient? httpClient, string apiKey)
        {
            _http = httpClient ?? new HttpClient();
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        }

        public async Task<string> PostJsonAsync(string endpoint, string jsonBody, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + endpoint)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation("ApiKey", _apiKey);

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new IranSmsException($"Ghasedak HTTP error ({(int)response.StatusCode}): {Truncate(body)}")
                {
                    ProviderName = "Ghasedak",
                    ProviderStatusCode = (int)response.StatusCode,
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
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new IranSmsException($"Ghasedak HTTP error ({(int)response.StatusCode}): {Truncate(body)}")
                {
                    ProviderName = "Ghasedak",
                    ProviderStatusCode = (int)response.StatusCode,
                    RawResponseBody = body,
                };
            }

            return body;
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}