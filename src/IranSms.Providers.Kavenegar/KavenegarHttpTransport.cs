namespace IranSms.Providers.Kavenegar
{
    /// <summary>
    /// Real Kavenegar transport backed by <see cref="HttpClient"/>.
    /// Posts form-urlencoded to https://api.kavenegar.com/v1/{api-key}/{method}.json.
    /// </summary>
    internal sealed class KavenegarHttpTransport : IKavenegarTransport, IDisposable
    {
        private const string BaseUrl = "https://api.kavenegar.com/v1";
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
            _http = httpClient ?? new HttpClient();
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
            var url = $"{BaseUrl}/{Uri.EscapeDataString(_apiKey)}/{method}.json";
            using (var content = new FormUrlEncodedContent(parameters))
            {
                using (var response = await _http.PostAsync(url, content, cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new IranSmsException(
                            $"Kavenegar HTTP error ({(int)response.StatusCode}): {Truncate(body)}")
                        {
                            ProviderName = "Kavenegar",
                            ProviderStatusCode = (int)response.StatusCode,
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
            var url = $"{BaseUrl}/{Uri.EscapeDataString(_apiKey)}/{method}.json";
            using (var response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false))
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new IranSmsException(
                        $"Kavenegar HTTP error ({(int)response.StatusCode}): {Truncate(body)}")
                    {
                        ProviderName = "Kavenegar",
                        ProviderStatusCode = (int)response.StatusCode,
                        RawResponseBody = body,
                    };
                }

                return body;
            }
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}
