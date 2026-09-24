using System.Net.Http.Headers;
using System.Text;

namespace IranSms.Providers.SmsIr
{
    /// <summary>
    /// Real SMS.ir transport backed by <see cref="HttpClient"/>.
    /// Sends HTTP calls to https://api.sms.ir/v1/{path} with the X-API-KEY header.
    /// </summary>
    internal sealed class SmsIrHttpTransport : ISmsIrTransport, IDisposable
    {
        private const string BaseUrl = "https://api.sms.ir/v1";
        private const long MaxResponseBytes = 1_048_576;
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly bool _ownsHttp;

        /// <summary>Initializes a new instance of the <see cref="SmsIrHttpTransport"/> class.</summary>
        /// <param name="apiKey">The SMS.ir X-API-KEY.</param>
        /// <param name="httpClient">Optional pre-configured <see cref="HttpClient"/> — caller-owned when supplied; transport-owned otherwise.</param>
        public SmsIrHttpTransport(string apiKey, HttpClient? httpClient = null)
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

        public void Dispose()
        {
            if (_ownsHttp)
                _http.Dispose();
        }

        /// <inheritdoc />
        public Task<string> PostJsonAsync(
            string path,
            string jsonBody,
            CancellationToken cancellationToken)
        {
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return SendAsync(HttpMethod.Post, path, content, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> GetAsync(string path, CancellationToken cancellationToken)
            => SendAsync(HttpMethod.Get, path, content: null, cancellationToken);

        private async Task<string> SendAsync(
            HttpMethod method,
            string path,
            HttpContent? content,
            CancellationToken cancellationToken)
        {
            var url = $"{BaseUrl}/{path}";

            using (content)
            using (var request = new HttpRequestMessage(method, url))
            {
                request.Headers.Add("X-API-KEY", _apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (content != null)
                    request.Content = content;

                using (var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false))
                {
                    if (response.Content.Headers.ContentLength > MaxResponseBytes)
                        throw new IranSmsException("SMS.ir response exceeded the maximum allowed size.")
                        {
                            ProviderName = "SmsIr",
                        };
                    var body = await ReadBodyAsync(response.Content, "SmsIr").ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new IranSmsException(
                             $"SMS.ir HTTP error ({(int)response.StatusCode}).")
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

        private static async Task<string> ReadBodyAsync(HttpContent content, string providerName)
        {
            if (content.Headers.ContentLength > MaxResponseBytes)
                throw new IranSmsException($"{providerName} response exceeded the maximum allowed size.")
                {
                    ProviderName = providerName,
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
                    };
                await memory.WriteAsync(buffer, 0, read).ConfigureAwait(false);
            }
            return Encoding.UTF8.GetString(memory.ToArray());
        }
    }
}
