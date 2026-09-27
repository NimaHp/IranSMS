
namespace IranSms
{
    /// <summary>
    /// Optional capability: OTP / template-based sends.
    /// Implemented by providers advertising <see cref="SmsCapabilities.OtpSend"/>.
    /// </summary>
    /// <remarks>
    /// Which request shape a provider accepts is declared separately:
    /// <see cref="SmsCapabilities.OtpTemplateSend"/> for <see cref="OtpTemplateRequest"/>
    /// and <see cref="SmsCapabilities.OtpCodeSend"/> for <see cref="OtpCodeRequest"/>.
    /// Check the matching flag before building a request — sending the wrong shape throws
    /// <see cref="ArgumentException"/>. The Mock provider supports both.
    /// </remarks>
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
