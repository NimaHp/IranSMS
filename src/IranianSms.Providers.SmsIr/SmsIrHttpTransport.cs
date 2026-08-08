using System.Net.Http.Headers;
using System.Text;

namespace IranianSms.Providers.SmsIr
{
    /// <summary>
    /// Real SMS.ir transport backed by <see cref="HttpClient"/>.
    /// Sends JSON to https://api.sms.ir/v1/{path} with the X-API-KEY header.
    /// </summary>
    internal sealed class SmsIrHttpTransport : ISmsIrTransport
    {
        private const string BaseUrl = "https://api.sms.ir/v1";
        private readonly HttpClient _http;
        private readonly string _apiKey;

        /// <summary>Initializes a new instance of the <see cref="SmsIrHttpTransport"/> class.</summary>
        /// <param name="apiKey">The SMS.ir X-API-KEY.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/>.</param>
        public SmsIrHttpTransport(string apiKey, HttpClient? httpClient = null)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _http = httpClient ?? new HttpClient();
        }

        /// <inheritdoc />
        public async Task<string> PostJsonAsync(
            string path,
            string? jsonBody,
            CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/{path}";
            HttpContent? content = null;
            if (jsonBody != null)
            {
                content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            using (content)
            {
                using (var request = new HttpRequestMessage(
                    jsonBody == null ? HttpMethod.Get : HttpMethod.Post, url))
                {
                    request.Headers.Add("X-API-KEY", _apiKey);
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    if (content != null)
                        request.Content = content;

                    using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {
                        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new IranianSmsException(
                                $"SMS.ir HTTP error ({(int)response.StatusCode}): {Truncate(body)}")
                            {
                                ProviderName = "SmsIr",
                                ProviderStatusCode = (int)response.StatusCode,
                                RawResponseBody = body,
                            };
                        }

                        return body;
                    }
                }
            }
        }

        private static string Truncate(string s, int max = 500)
            => s.Length <= max ? s : s.Substring(0, max);
    }
}