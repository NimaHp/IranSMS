namespace IranSms.Providers.Ghasedak
{
    /// <summary>
    /// Contract for the Ghasedak HTTP transport, so tests can inject
    /// scripted responses without a real network call.
    /// </summary>
    internal interface IGhasedakTransport
    {
        /// <summary>POSTs a JSON body to a Ghasedak WebService endpoint and returns the raw response body.</summary>
        Task<string> PostJsonAsync(string endpoint, string jsonBody, CancellationToken cancellationToken);

        /// <summary>GETs a Ghasedak WebService endpoint with query parameters and returns the raw response body.</summary>
        Task<string> GetAsync(string endpoint, IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken);
    }
}