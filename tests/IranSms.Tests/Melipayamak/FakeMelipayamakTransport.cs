using IranSms.Providers.Melipayamak;

namespace IranSms.Tests.Melipayamak
{
    /// <summary>
    /// Scripted in-memory transport for Melipayamak tests — returns canned
    /// plain-text bodies, records the last request, or throws on demand.
    /// </summary>
    internal sealed class FakeMelipayamakTransport : IMelipayamakTransport
    {
        public string? ResponseBody { get; set; }
        public Exception? ExceptionToThrow { get; set; }
        public string? LastAction { get; private set; }
        public IReadOnlyDictionary<string, string>? LastForm { get; private set; }
        public int CallCount { get; private set; }

        public Task<string> PostFormAsync(
            string action,
            IReadOnlyDictionary<string, string> form,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastAction = action;
            LastForm = form;

            if (ExceptionToThrow != null)
                throw ExceptionToThrow;

            return Task.FromResult(ResponseBody ?? "1");
        }
    }
}
