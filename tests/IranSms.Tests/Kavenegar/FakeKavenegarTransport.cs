using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IranSms.Providers.Kavenegar;

namespace IranSms.Tests.Kavenegar
{
    /// <summary>
    /// Scripted in-memory transport for Kavenegar tests — returns canned bodies,
    /// records the last request, or throws on demand.
    /// </summary>
    internal sealed class FakeKavenegarTransport : IKavenegarTransport
    {
        public string? ResponseBody { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public string? LastMethod { get; private set; }
        public IReadOnlyDictionary<string, string>? LastParameters { get; private set; }
        public int CallCount { get; private set; }

        public Task<string> PostAsync(
            string method,
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastMethod = method;
            LastParameters = parameters;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.FromResult(ResponseBody ?? "{\"return\":{\"status\":200,\"message\":\"OK\"},\"entries\":[]}");
        }
    }
}