
namespace IranSms
{
    /// <summary>
    /// Base type for OTP / verification sends. A verification send is either
    /// template-based — a pre-approved provider pattern filled with parameters
    /// (<see cref="OtpTemplateRequest"/>) — or code-based, where the provider
    /// injects the code into a fixed service text (<see cref="OtpCodeRequest"/>).
    /// </summary>
    /// <remarks>
    /// These are two different concepts and are deliberately separate types: a template
    /// is a provider-side registered pattern, while an OTP is the purpose of the message.
    /// Providers that need a template accept <see cref="OtpTemplateRequest"/>; providers
    /// that own the message text accept <see cref="OtpCodeRequest"/>. Fields common to
    /// both shapes live here: <see cref="SenderLine"/>, <see cref="SendDate"/> and
    /// <see cref="ClientReferenceId"/>.
    /// </remarks>
    public abstract class OtpRequest
    {
        /// <summary>Optional sender line (falls back to provider default when null).</summary>
        public string? SenderLine { get; set; }

        /// <summary>
        /// Optional scheduled send date-time (provider must support
        /// <see cref="SmsCapabilities.ScheduledSend"/>). No provider currently honours
        /// this; setting it throws <see cref="NotSupportedException"/> at send time — use a
        /// scheduler in your application instead.
        /// </summary>
        public DateTimeOffset? SendDate { get; set; }

        /// <summary>
        /// Optional client-supplied reference (idempotency / correlation key). Providers
        /// that support a local reference forward it so retries of the same logical send
        /// can be de-duplicated and correlated; providers without reference support ignore
        /// it. Validate with <see cref="SmsValidation.EnsureClientReferenceId"/>.
        /// </summary>
        public string? ClientReferenceId { get; set; }
    }
}
