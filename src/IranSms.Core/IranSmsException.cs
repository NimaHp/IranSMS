namespace IranSms
{
    /// <summary>
    /// Thrown by provider clients when the SMS API rejects a request (HTTP
    /// error or an API-level error code), or when a provider response is
    /// malformed. Transport-level failures (connection refused, timeout)
    /// surface as <see cref="System.Net.Http.HttpRequestException"/>.
    /// </summary>
    /// <remarks>
    /// Error handling: caller-side (input) problems are reported as
    /// <see cref="ArgumentException"/> / <see cref="ArgumentNullException"/> by
    /// <see cref="SmsValidation"/>; provider, transport and protocol problems are
    /// reported as <see cref="IranSmsException"/> with a normalized
    /// <see cref="Kind"/> and the provider-specific <see cref="ProviderStatusCode"/>.
    /// Capability gaps (for example <see cref="OtpRequest.SendDate"/> on a provider
    /// without <see cref="SmsCapabilities.ScheduledSend"/>) keep surfacing as
    /// <see cref="NotSupportedException"/>.
    /// </remarks>
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

        /// <summary>
        /// Gets or sets the normalized failure category (<see cref="SmsErrorKind"/>).
        /// Defaults to <see cref="SmsErrorKind.Unknown"/> for legacy call sites that
        /// do not classify their failures yet.
        /// </summary>
        public SmsErrorKind Kind { get; set; } = SmsErrorKind.Unknown;

        /// <summary>
        /// Gets or sets the logical operation that failed (e.g. "SendBulk", "Lookup").
        /// Purely diagnostic — never contains recipients, message text or credentials.
        /// </summary>
        public string? Operation { get; set; }

        /// <summary>
        /// Gets a value indicating whether repeating the same request may succeed later,
        /// derived from <see cref="Kind"/>. Callers must still respect
        /// <see cref="SmsErrorKindExtensions.IsTransient"/> semantics for cancellation
        /// and for sends that carry no idempotency key.
        /// </summary>
        public bool IsTransient => Kind.IsTransient();

        /// <summary>
        /// Creates an exception for a business-level rejection returned by the provider API.
        /// </summary>
        /// <param name="message">Diagnostic message (must not contain secrets, recipients or message text).</param>
        /// <param name="providerStatusCode">Provider-specific status code.</param>
        /// <param name="providerName">Provider name.</param>
        /// <param name="operation">Logical operation that was attempted.</param>
        /// <returns>A configured <see cref="IranSmsException"/>.</returns>
        public static IranSmsException ProviderRejected(
            string message,
            int? providerStatusCode = null,
            string? providerName = null,
            string? operation = null)
        {
            return new IranSmsException(message)
            {
                ProviderName = providerName,
                ProviderStatusCode = providerStatusCode,
                Operation = operation,
                Kind = SmsErrorKind.ProviderRejected,
            };
        }

        /// <summary>
        /// Creates an exception for a throttled request.
        /// </summary>
        /// <param name="message">Diagnostic message.</param>
        /// <param name="providerName">Provider name.</param>
        /// <param name="operation">Logical operation that was attempted.</param>
        /// <returns>A configured <see cref="IranSmsException"/> with a transient <see cref="Kind"/>.</returns>
        public static IranSmsException RateLimited(string message, string? providerName = null, string? operation = null)
        {
            return new IranSmsException(message)
            {
                ProviderName = providerName,
                Operation = operation,
                Kind = SmsErrorKind.RateLimited,
            };
        }

        /// <summary>
        /// Creates an exception for a response that could not be parsed or lacked required fields.
        /// </summary>
        /// <param name="message">Diagnostic message.</param>
        /// <param name="innerException">Optional parser/protocol exception.</param>
        /// <param name="providerName">Provider name.</param>
        /// <param name="rawResponseBody">Raw body for diagnostics; handle with care because it may contain recipients.</param>
        /// <param name="operation">Logical operation that was attempted.</param>
        /// <returns>A configured <see cref="IranSmsException"/>.</returns>
        public static IranSmsException MalformedResponse(
            string message,
            Exception? innerException = null,
            string? providerName = null,
            string? rawResponseBody = null,
            string? operation = null)
        {
            var exception = innerException is null
                ? new IranSmsException(message)
                : new IranSmsException(message, innerException);

            exception.ProviderName = providerName;
            exception.RawResponseBody = rawResponseBody;
            exception.Operation = operation;
            exception.Kind = SmsErrorKind.MalformedResponse;
            return exception;
        }
    }
}
