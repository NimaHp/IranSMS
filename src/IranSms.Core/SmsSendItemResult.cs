
namespace IranSms
{
    /// <summary>
    /// Outcome of one recipient inside a batch send: a batch never fails as a whole
    /// just because one recipient was rejected.
    /// </summary>
    public sealed class SmsSendItemResult
    {
        private SmsSendItemResult(
            string recipient,
            string? messageId,
            SmsErrorKind? errorKind,
            string? errorMessage,
            int? providerStatusCode)
        {
            Recipient = recipient;
            MessageId = messageId;
            ErrorKind = errorKind;
            ErrorMessage = errorMessage;
            ProviderStatusCode = providerStatusCode;
        }

        /// <summary>Destination number this outcome refers to (as supplied by the caller).</summary>
        public string Recipient { get; }

        /// <summary>Provider-assigned message id; null when the send failed.</summary>
        public string? MessageId { get; }

        /// <summary>Normalized failure category; null when the send succeeded.</summary>
        public SmsErrorKind? ErrorKind { get; }

        /// <summary>Diagnostic failure text; null when the send succeeded. Never carries secrets.</summary>
        public string? ErrorMessage { get; }

        /// <summary>Provider-specific status code for a failed item; null when not reported or successful.</summary>
        public int? ProviderStatusCode { get; }

        /// <summary>Indicates whether this recipient was accepted by the provider.</summary>
        public bool Succeeded => ErrorKind is null;

        /// <summary>Indicates whether the provider explicitly reported a transient failure.</summary>
        public bool IsTransient => ErrorKind.HasValue && ErrorKind.Value.IsTransient();

        /// <summary>
        /// Creates a successful outcome for one recipient.
        /// </summary>
        /// <param name="recipient">Destination number.</param>
        /// <param name="messageId">Provider-assigned message id, when the provider reports one.</param>
        /// <returns>A successful <see cref="SmsSendItemResult"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="recipient"/> is null or whitespace.</exception>
        public static SmsSendItemResult Success(string recipient, string? messageId = null)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException("Recipient is required.", nameof(recipient));

            return new SmsSendItemResult(recipient, messageId, null, null, null);
        }

        /// <summary>
        /// Creates a failed outcome for one recipient.
        /// </summary>
        /// <param name="recipient">Destination number.</param>
        /// <param name="errorMessage">Diagnostic failure text; must not contain secrets or message text.</param>
        /// <param name="errorKind">Normalized failure category.</param>
        /// <param name="providerStatusCode">Provider-specific status code, when reported.</param>
        /// <returns>A failed <see cref="SmsSendItemResult"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="recipient"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="errorMessage"/> is null.</exception>
        public static SmsSendItemResult Failure(
            string recipient,
            string errorMessage,
            SmsErrorKind errorKind = SmsErrorKind.ProviderRejected,
            int? providerStatusCode = null)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException("Recipient is required.", nameof(recipient));
            if (errorMessage is null)
                throw new ArgumentNullException(nameof(errorMessage));

            return new SmsSendItemResult(recipient, null, errorKind, errorMessage, providerStatusCode);
        }
    }
}
