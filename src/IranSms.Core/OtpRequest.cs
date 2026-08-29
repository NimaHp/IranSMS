
namespace IranSms
{
    /// <summary>
    /// Request payload for OTP / verification sends.
    /// Models BOTH template-based providers (Kavenegar verify/lookup,
    /// SMS.ir send/verify templateId+parameters, Ghasedak SendOtpSMS) and
    /// code-based providers (Melipayamak SendOtp where a numeric code is
    /// embedded in a fixed service text).
    /// </summary>
    public sealed class OtpRequest
    {
        /// <summary>Template identifier — provider-specific: templateId (SMS.ir), template name (Ghasedak/Kavenegar), BodyId (Melipayamak pattern).</summary>
        public string? TemplateId { get; set; }

        /// <summary>Template parameters (template-based providers).</summary>
        public IReadOnlyDictionary<string, string>? Parameters { get; set; }

        /// <summary>Numeric OTP code (code-based providers — e.g. Melipayamak).</summary>
        public string? Code { get; set; }

        /// <summary>Optional sender line (falls back to provider default when null).</summary>
        public string? SenderLine { get; set; }

        /// <summary>Optional scheduled send date-time (provider must support <see cref="SmsCapabilities.ScheduledSend"/>).</summary>
        public DateTimeOffset? SendDate { get; set; }
    }
}
