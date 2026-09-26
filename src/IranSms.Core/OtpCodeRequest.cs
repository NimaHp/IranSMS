
namespace IranSms
{
    /// <summary>
    /// Code-based OTP request: the provider owns the message text and injects the numeric
    /// code into a fixed service message. Used by providers whose OTP endpoint takes the
    /// code directly instead of a template (for example Melipayamak <c>SendOtp</c>,
    /// which takes <c>username</c>, <c>password</c>, <c>from</c>, <c>to</c> and <c>code</c>).
    /// </summary>
    public sealed class OtpCodeRequest : OtpRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OtpCodeRequest"/> class.
        /// </summary>
        /// <param name="code">The verification code to send.</param>
        /// <exception cref="ArgumentException"><paramref name="code"/> is null or whitespace.</exception>
        public OtpCodeRequest(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Code is required.", nameof(code));

            Code = code;
        }

        /// <summary>The verification code handed to the provider.</summary>
        public string Code { get; }
    }
}
