using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IranianSms.Providers.Ghasedak;

namespace IranianSms.Tests.Ghasedak
{
    /// <summary>
    /// Scripted in-memory transport for Ghasedak tests — returns canned
    /// JSON bodies, records requests, or throws.
    /// </summary>
    internal sealed class FakeGhasedakTransport : IGhasedakTransport
    {
        public string? PostResponse { get; set; }
        public string? GetResponse { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public string? LastEndpoint { get; private set; }
        public string? LastJsonBody { get; private set; }
        public IReadOnlyDictionary<string, string>? LastQuery { get; private set; }
        public int PostCount { get; private set; }
        public int GetCount { get; private set; }

        public Task<string> PostJsonAsync(string endpoint, string jsonBody, CancellationToken cancellationToken)
        {
            PostCount++;
            LastEndpoint = endpoint;
            LastJsonBody = jsonBody;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.FromResult(GetBody(PostResponse, endpoint));
        }

        public Task<string> GetAsync(string endpoint, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
        {
            GetCount++;
            LastEndpoint = endpoint;
            LastQuery = query;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.FromResult(GetResponse ?? "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":[]}");
        }

        private static string GetBody(string? body, string endpoint)
            => body ?? DefaultBody(endpoint);

        private static string DefaultBody(string endpoint)
            => endpoint == "CheckSmsStatus"
                ? "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":[]}"
                : "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"MessageId\":\"gh-123\"}}";
    }
}