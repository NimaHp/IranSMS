
namespace IranSms
{
    /// <summary>
    /// Optional capability: query the delivery status of previously sent messages.
    /// Advertised by providers with <see cref="SmsCapabilities.DeliveryStatus"/>.
    /// </summary>
    public interface ISmsDeliveryReporter
    {
        /// <summary>
        /// Gets the delivery status of a sent message.
        /// </summary>
        /// <param name="message">Message identifier (provider or client reference id).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<MessageStatusResult> GetMessageStatusAsync(
            MessageIdentifier message,
            CancellationToken cancellationToken = default);
    }
}
