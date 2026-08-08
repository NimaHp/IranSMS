
namespace IranianSms
{
    /// <summary>
    /// Result of a successful send. SmsId is a transport-neutral opaque
    /// identifier for delivery lookups (providers map it to messageid/recId/...).
    /// </summary>
    public sealed class SmsSendResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmsSendResult"/> class.
        /// </summary>
        /// <param name="messageId">Provider-assigned message identifier (opaque).</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageId"/> is null.</exception>
        public SmsSendResult(string messageId)
        {
            MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
        }

        /// <summary>Provider message id (opaque).</summary>
        public string MessageId { get; }

        /// <summary>Optional cost of the send (provider-specific currency/unit).</summary>
        public decimal? Cost { get; set; }

        /// <summary>Optional per-recipient ids (bulk/heterogeneous sends).</summary>
        public string[]? RecipientIds { get; set; }
    }
}