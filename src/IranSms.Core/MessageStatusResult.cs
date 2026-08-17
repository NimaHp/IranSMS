
namespace IranSms
{
    /// <summary>
    /// Delivery state of a sent message (provider-normalized superset).
    /// </summary>
    public enum MessageDeliveryState
    {
        /// <summary>No status / unknown.</summary>
        Unknown = 0,

        /// <summary>Queued / accepted by the provider.</summary>
        Queued = 1,

        /// <summary>Scheduled for a future date.</summary>
        Scheduled = 2,

        /// <summary>Sent to the operator (in transit).</summary>
        SentToOperator = 3,

        /// <summary>Delivered to the recipient device.</summary>
        Delivered = 4,

        /// <summary>Failed / rejected by the provider or operator.</summary>
        Failed = 5,

        /// <summary>Cancelled.</summary>
        Cancelled = 6,

        /// <summary>Blocklisted (provider or operator).</summary>
        Blocked = 7,

        /// <summary>Expired without delivery.</summary>
        Undelivered = 8,
    }

    /// <summary>
    /// Normalized delivery-status payload for one message.
    /// </summary>
    public sealed class MessageStatusResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageStatusResult"/> class.
        /// </summary>
        /// <param name="state">The normalized delivery state.</param>
        /// <param name="identifier">The identifier this result refers to.</param>
        public MessageStatusResult(MessageDeliveryState state, MessageIdentifier identifier)
        {
            State = state;
            Identifier = identifier;
        }

        /// <summary>Normalized delivery state.</summary>
        public MessageDeliveryState State { get; }

        /// <summary>Identifier this result refers to.</summary>
        public MessageIdentifier Identifier { get; }

        /// <summary>Raw provider status code (useful when the normalized value is <see cref="MessageDeliveryState.Unknown"/>).</summary>
        public string? RawStatus { get; set; }

        /// <summary>Optional recipient (provider-provided).</summary>
        public string? Recipient { get; set; }

        /// <summary>Optional provider price of the message.</summary>
        public decimal? Price { get; set; }

        /// <summary>Optional send date (provider-provided).</summary>
        public DateTimeOffset? SendDate { get; set; }

        /// <summary>Optional message text (provider-provided).</summary>
        public string? MessageText { get; set; }
    }
}