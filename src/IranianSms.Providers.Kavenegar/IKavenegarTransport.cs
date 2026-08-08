namespace IranianSms.Providers.Kavenegar
{
    /// <summary>
    /// Contract for the Kavenegar HTTP transport, so tests can inject
    /// scripted responses without a real network call.
    /// </summary>
    internal interface IKavenegarTransport
    {
        /// <summary>Posts a form-urlencoded request to the given method path.</summary>
        /// <param name="method">The Kavenegar method (e.g. <c>sms/send</c>).</param>
        /// <param name="parameters">The form parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The raw response body.</returns>
        Task<string> PostAsync(string method, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken);
    }
}