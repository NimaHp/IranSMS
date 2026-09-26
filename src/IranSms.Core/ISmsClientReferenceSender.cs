
namespace IranSms
{
    /// <summary>
    /// Optional capability: send a message with a client-supplied reference so the
    /// caller can correlate the send in its own database — and, on providers with
    /// <see cref="SmsCapabilities.ClientReferenceLookup"/>, query delivery status
    /// without storing the provider message id.
    /// </summary>
    /// <remarks>
    /// Additive alternative to changing <see cref="ISmsClient.SendAsync"/>: implement
    /// this interface on clients that advertise <see cref="SmsCapabilities.ClientReference"/>,
    /// and discover it with <c>client is ISmsClientReferenceSender</c>.
    /// Reference semantics are provider-specific: Kavenegar expects a numeric
    /// <c>localid</c> and de-duplicates repeated values server-side, while Ghasedak
    /// accepts an opaque string without a documented de-duplication guarantee.
    /// </remarks>
    public interface ISmsClientReferenceSender
    {
        /// <summary>
        /// Sends a single SMS together with a client-supplied reference.
        /// </summary>
        /// <param name="recipient">Destination number; normalized by <see cref="SmsValidation"/>.</param>
        /// <param name="message">Message text.</param>
        /// <param name="clientReferenceId">Caller-generated reference; never sent to the recipient.</param>
        /// <param name="senderLine">Optional sender line; provider default when null.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The send result, including <see cref="SmsSendResult.ClientReferenceId"/> when the provider echoes it.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="recipient"/>, <paramref name="message"/> or <paramref name="clientReferenceId"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="clientReferenceId"/> is blank, or the provider requires a specific format (for example Kavenegar requires a numeric <c>localid</c>).</exception>
        Task<SmsSendResult> SendWithReferenceAsync(
            string recipient,
            string message,
            string clientReferenceId,
            string? senderLine = null,
            CancellationToken cancellationToken = default);
    }
}
