
namespace IranSms
{
    /// <summary>
    /// Capabilities a provider can support, as a power-of-two flags enum.
    /// Check support WITHOUT <c>Enum.HasFlag</c> (it boxes on netstandard2.0):
    /// <code>(client.Capabilities &amp; SmsCapabilities.Send) == SmsCapabilities.Send</code>
    /// </summary>
    [Flags]
    public enum SmsCapabilities : long
    {
        /// <summary>No capabilities (fallback / unknown provider).</summary>
        None = 0,

        /// <summary>Single SMS send (one text to one destination).</summary>
        Send = 1 << 0,

        /// <summary>Bulk send — one text to many destinations in a single call.</summary>
        BulkSend = 1 << 1,

        /// <summary>Heterogeneous send — distinct text per destination.</summary>
        HeterogeneousSend = 1 << 2,

        /// <summary>Scheduled (future-dated) send.</summary>
        ScheduledSend = 1 << 3,

        /// <summary>OTP / template-based verification send.</summary>
        OtpSend = 1 << 4,

        /// <summary>Query delivery status of previously sent messages.</summary>
        DeliveryStatus = 1 << 5,

        /// <summary>Enumerate sent-message history / reports.</summary>
        MessageHistory = 1 << 6,

        /// <summary>Receive (inbox) of incoming messages.</summary>
        Receive = 1 << 7,

        /// <summary>Account info / credit queries.</summary>
        AccountInfo = 1 << 8,

        /// <summary>Sender line management (list/block/unblock).</summary>
        LineManagement = 1 << 9,

        /// <summary>Template CRUD (create/read/update/delete OTP patterns).</summary>
        TemplateManagement = 1 << 10,

        /// <summary>Flash (pop-up) message support.</summary>
        FlashMessage = 1 << 11,

        /// <summary>Voice message support.</summary>
        VoiceMessage = 1 << 12,

        /// <summary>Read-only inspection of OTP template parameters (e.g. Ghasedak GetOtpTemplateParameters).</summary>
        OtpTemplateInspection = 1 << 13,
    }
}