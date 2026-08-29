namespace IranSms.Providers.Melipayamak
{
    /// <summary>
    /// Contract for the Melipayamak HTTP transport, so tests can inject
    /// scripted plain-text responses without a real network call.
    /// </summary>
    internal interface IMelipayamakTransport
    {
        /// <summary>Posts form-urlencoded data to the given action path and returns the raw body.</summary>
        /// <param name="action">The API action (e.g. <c>SendSMS</c>, <c>SendOtp</c>).</param>
        /// <param name="form">The form fields (username/password/to/text/...).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The raw response body (usually a numeric recId).</returns>
        Task<string> PostFormAsync(string action, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken);
    }
}
