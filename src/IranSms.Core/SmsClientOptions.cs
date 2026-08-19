namespace IranSms
{
    /// <summary>
    /// Options common to every provider client when registered through the DI
    /// convenience layer. A plain POCO on purpose: Core carries zero external
    /// dependencies, so this base never references Microsoft.Extensions.Options.
    /// Concrete provider options (e.g. <c>KavenegarOptions</c>) derive from it and
    /// add their provider-specific credentials.
    /// </summary>
    public abstract class SmsClientOptions
    {
        /// <summary>
        /// Optional nominal connection timeout applied to the typed
        /// <see cref="System.Net.Http.HttpClient"/> created through
        /// <c>IHttpClientFactory</c>. Null leaves the factory default.
        /// </summary>
        public TimeSpan? Timeout { get; set; }
    }
}