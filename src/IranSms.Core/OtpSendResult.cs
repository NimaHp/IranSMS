
namespace IranSms
{
    /// <summary>
    /// Result of an OTP send — the message id plus optional cost.
    /// </summary>
    public sealed class OtpSendResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OtpSendResult"/> class.
        /// </summary>
        /// <param name="messageId">Provider-assigned message identifier.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageId"/> is null.</exception>
        public OtpSendResult(string messageId)
        {
            MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
        }

        /// <summary>Gets the provider-assigned message identifier (opaque).</summary>
        public string MessageId { get; }

        /// <summary>Gets or sets the optional cost of the send (provider-specific currency/unit).</summary>
        public decimal? Cost { get; set; }

        /// <summary>
        /// Gets or sets the client reference that was accepted by the provider, when the
        /// provider supports client references. Null means the provider returned no
        /// reference (or does not support them).
        /// </summary>
        public string? ClientReferenceId { get; set; }
    }
}
