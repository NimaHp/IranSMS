
namespace IranSms
{
    /// <summary>
    /// Base contract for every IranSms provider client.
    /// All providers implement this; optional capabilities are surfaced via
    /// capability-specific interfaces (e.g. <see cref="ISmsOtpSender"/>,
    /// <see cref="ISmsBulkSender"/>) and discoverable through
    /// <c>client as ISmsOtpSender</c> / <c>client.Supports(SmsCapabilities.OtpSend)</c>.
    /// </summary>
    public interface ISmsClient
    {
        /// <summary>Provider name (e.g. "Kavenegar").</summary>
        string ProviderName { get; }

        /// <summary>Raw capabilities bitmask — check with <c>(caps &amp; flag) == flag</c> (not <c>HasFlag</c>).</summary>
        SmsCapabilities Capabilities { get; }

        /// <summary>
        /// Sends a single SMS.
        /// </summary>
        /// <param name="recipient">Destination phone number.</param>
        /// <param name="message">Message text.</param>
        /// <param name="senderLine">Optional sender line; provider default when null.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<SmsSendResult> SendAsync(
            string recipient,
            string message,
            string? senderLine = null,
            CancellationToken cancellationToken = default);
    }
}
