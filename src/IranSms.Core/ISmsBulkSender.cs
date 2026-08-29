
namespace IranSms
{
    /// <summary>
    /// Optional capability: send one text to many recipients in a single call.
    /// Advertised by providers with <see cref="SmsCapabilities.BulkSend"/>.
    /// </summary>
    public interface ISmsBulkSender
    {
        /// <summary>
        /// Sends a single message to multiple recipients.
        /// </summary>
        /// <param name="recipients">Destination phone numbers.</param>
        /// <param name="message">Message text.</param>
        /// <param name="senderLine">Optional sender line.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<SmsSendResult> SendBulkAsync(
            IEnumerable<string> recipients,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default);
    }
}
