using IranianSms.Providers.SmsIr;

namespace IranianSms.Tests.SmsIr
{
    /// <summary>
    /// Scripted in-memory transport for SMS.ir tests — returns canned bodies,
    /// records the last request, or throws on demand.
    /// </summary>
    internal sealed class FakeSmsIrTransport : ISmsIrTransport
    {
        public string? ResponseBody { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public string? LastPath { get; private set; }
        public string? LastJson { get; private set; }
        public int CallCount { get; private set; }

        public Task<string> PostJsonAsync(
            string path,
            string jsonBody,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastPath = path;
            LastJson = jsonBody;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.FromResult(ResponseBody ?? "{\"status\":1,\"message\":\"موفق\",\"data\":{}}");
        }

        public Task<string> GetAsync(string path, CancellationToken cancellationToken)
        {
            CallCount++;
            LastPath = path;
            LastJson = null;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.FromResult(ResponseBody ?? "{\"status\":1,\"message\":\"موفق\",\"data\":{}}");
        }
    }
}
