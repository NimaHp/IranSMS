namespace IranianSms.Providers.SmsIr
{
    /// <summary>
    /// Contract for the SMS.ir HTTP transport, so tests can inject scripted
    /// responses without a real network call.
    /// </summary>
    internal interface ISmsIrTransport
    {
        /// <summary>Posts JSON to the given path (relative to <c>/v1</c>) with the API key header.</summary>
        /// <param name="path">The endpoint path, e.g. <c>send</c> or <c>send/bulk</c>.</param>
        /// <param name="jsonBody">The JSON payload, or null for GET requests.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The raw response body.</returns>
        Task<string> PostJsonAsync(string path, string? jsonBody, CancellationToken cancellationToken);
    }
}