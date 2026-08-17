namespace IranSms.Providers.Melipayamak
{
    /// <summary>
    /// Real Melipayamak transport backed by <see cref="HttpClient"/>.
    /// Posts form-urlencoded to https://rest.payamak-panel.com/api/SendSMS/{action}.
    /// </summary>
    internal sealed class MelipayamakHttpTransport : IMelipayamakTransport
    {
        private const string BaseUrl = "https://rest.payamak-panel.com/api/SendSMS";
        private readonly HttpClient _http;

        /// <summary>Initializes a new instance of the <see cref="MelipayamakHttpTransport"/> class.</summary>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>.</param>
        public MelipayamakHttpTransport(HttpClient? httpClient = null)
        {
            _http = httpClient ?? new HttpClient();
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
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new IranSmsException(
                            $"Melipayamak HTTP error ({(int)response.StatusCode}): {Truncate(body)}")
                        {
                            ProviderName = "Melipayamak",
                            ProviderStatusCode = (int)response.StatusCode,
                            RawResponseBody = body,
                        };
                    }

                    return body;
                }
            }
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}