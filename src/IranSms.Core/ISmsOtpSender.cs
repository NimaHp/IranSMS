
namespace IranSms
{
    /// <summary>
    /// Optional capability: OTP / template-based sends.
    /// Implemented by providers advertising <see cref="SmsCapabilities.OtpSend"/>.
    /// </summary>
    public interface ISmsOtpSender
    {
        /// <summary>
        /// Sends an OTP / verification SMS.
        /// </summary>
        /// <param name="recipient">Destination mobile number.</param>
        /// <param name="request">OTP payload (template or code semantics).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task<OtpSendResult> SendOtpAsync(
            string recipient,
            OtpRequest request,
            CancellationToken cancellationToken = default);
    }
}