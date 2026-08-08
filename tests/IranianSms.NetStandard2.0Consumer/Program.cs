// Compile-time guard: consumes the IranianSms.Core public API from an
// old-style netstandard2.0 project (LangVersion 7.3, nullable disabled).
// If the Core API leaks modern C# features (record/init/required/DateOnly),
// this project fails to compile.

using System;

namespace IranianSms.NetStandard2Consumer
{
    internal static class Program
    {
        private static int Main()
        {
            // 1. Capabilities — non-boxing bit check
            var caps = SmsCapabilities.Send | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus;
            bool supportsOtp = (caps & SmsCapabilities.OtpSend) == SmsCapabilities.OtpSend;
            Console.WriteLine("SupportsOtp: " + supportsOtp);

            // 2. MessageIdentifier value object
            var id = new MessageIdentifier("rec-123", MessageIdentifierType.ProviderMessageId);
            Console.WriteLine("Id: " + id);

            // 3. OtpRequest with template semantics
            var otp = new OtpRequest
            {
                TemplateId = "tmpl-1",
                Code = null,
                SenderLine = "3000"
            };
            Console.WriteLine("TemplateId: " + otp.TemplateId);

            // 4. Send result + status result
            var send = new SmsSendResult("msg-1");
            Console.WriteLine("SendId: " + send.MessageId);

            var status = new MessageStatusResult(MessageDeliveryState.Delivered, id);
            Console.WriteLine("State: " + status.State);

            return supportsOtp ? 0 : 1;
        }
    }
}