
namespace IranSms
{
    /// <summary>
    /// Transport-neutral classification of a failure surfaced by a provider client.
    /// Provider-specific codes stay available on
    /// <see cref="IranSmsException.ProviderStatusCode"/>; this enum only states the
    /// category so callers can branch (retry, refresh credentials, surface to the user)
    /// without any provider knowledge.
    /// </summary>
    public enum SmsErrorKind
    {
        /// <summary>Unclassified failure.</summary>
        Unknown = 0,

        /// <summary>The provider rejected the request because of its content or arguments (4xx-style business error).</summary>
        ProviderRejected = 1,

        /// <summary>Missing, invalid or expired credentials (API key, token, account).</summary>
        Unauthorized = 2,

        /// <summary>The provider throttled the request (rate limit / quota exceeded).</summary>
        RateLimited = 3,

        /// <summary>The account has no credit or the requested service is not available for it.</summary>
        InsufficientBalance = 4,

        /// <summary>The provider answer could not be parsed or lacked required fields.</summary>
        MalformedResponse = 5,

        /// <summary>The request never completed at the transport level (connection reset, DNS failure, TLS failure).</summary>
        Transport = 6,

        /// <summary>The request exceeded the configured timeout.</summary>
        Timeout = 7,

        /// <summary>The caller cancelled the request.</summary>
        Cancelled = 8,

        /// <summary>The provider answered with a server-side error (HTTP 5xx) and is temporarily unavailable.</summary>
        ProviderUnavailable = 9,
    }

    /// <summary>
    /// Classification helpers for <see cref="SmsErrorKind"/>.
    /// </summary>
    public static class SmsErrorKindExtensions
    {
        /// <summary>
        /// Determines whether repeating the exact same request may succeed later.
        /// </summary>
        /// <param name="kind">The error classification.</param>
        /// <returns>
        /// <see langword="true"/> for <see cref="SmsErrorKind.RateLimited"/>,
        /// <see cref="SmsErrorKind.Transport"/>, <see cref="SmsErrorKind.Timeout"/>
        /// and <see cref="SmsErrorKind.ProviderUnavailable"/>.
        /// </returns>
        /// <remarks>
        /// <see cref="SmsErrorKind.Cancelled"/> is never transient here: cancellation is a
        /// caller decision, and replaying a send without an idempotency key can duplicate
        /// messages. Callers must opt in explicitly.
        /// </remarks>
        public static bool IsTransient(this SmsErrorKind kind)
        {
            switch (kind)
            {
                case SmsErrorKind.RateLimited:
                case SmsErrorKind.Transport:
                case SmsErrorKind.Timeout:
                case SmsErrorKind.ProviderUnavailable:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Maps an HTTP status code to <see cref="SmsErrorKind"/>. Used by every transport
        /// so identical status codes are classified identically across providers, and by
        /// providers whose envelope status mirrors HTTP semantics.
        /// </summary>
        /// <param name="statusCode">HTTP status code reported by the provider.</param>
        /// <returns>The normalized error category.</returns>
        public static SmsErrorKind FromHttpStatus(int statusCode)
        {
            switch (statusCode)
            {
                case 401:
                case 403:
                    return SmsErrorKind.Unauthorized;
                case 402:
                    return SmsErrorKind.InsufficientBalance;
                case 408:
                case 504:
                    return SmsErrorKind.Timeout;
                case 429:
                    return SmsErrorKind.RateLimited;
                default:
                    if (statusCode >= 500)
                        return SmsErrorKind.ProviderUnavailable;
                    return SmsErrorKind.ProviderRejected;
            }
        }
    }
}
