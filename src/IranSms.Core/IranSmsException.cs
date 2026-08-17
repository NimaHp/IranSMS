namespace IranSms
{
    /// <summary>
    /// Thrown by provider clients when the SMS API rejects a request (HTTP
    /// error or an API-level error code), or when a provider response is
    /// malformed. Transport-level failures (connection refused, timeout)
    /// surface as <see cref="System.Net.Http.HttpRequestException"/>.
    /// </summary>
    public class IranSmsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IranSmsException"/> class.
        /// </summary>
        public IranSmsException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IranSmsException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public IranSmsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IranSmsException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The exception that caused this error.</param>
        public IranSmsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Gets or sets the provider name (e.g. "Kavenegar") that produced the error.</summary>
        public string? ProviderName { get; set; }

        /// <summary>Gets or sets the provider status code (e.g. Kavenegar 424), or null when the transport itself failed.</summary>
        public int? ProviderStatusCode { get; set; }

        /// <summary>Gets or sets the raw provider response body when available.</summary>
        public string? RawResponseBody { get; set; }
    }
}