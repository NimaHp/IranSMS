using IranSms;
using IranSms.Providers.Mock;

// Basic sample: direct usage, no DI container required.
// The Mock provider runs fully in-memory (no network, no credentials),
// which makes it ideal for local experiments and demos.

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var cancellationToken = cts.Token;

var mock = new MockSmsClient("Demo");
Console.WriteLine($"Provider: {mock.ProviderName}");
Console.WriteLine($"Capabilities: {mock.Capabilities}");

// 1. Single send
var single = await mock.SendAsync(
    recipient: "09121234567",
    message: "Hello from IranSms!",
    cancellationToken: cancellationToken);
Console.WriteLine($"Single send -> {single.MessageId} (cost: {single.Cost})");

// 2. Capability-aware dispatch without hard casts
if (mock is ISmsBulkSender bulk)
{
    var result = await bulk.SendBulkAsync(
        recipients: new[] { "09121234567", "09351111111" },
        message: "Bulk hello!",
        cancellationToken: cancellationToken);
    Console.WriteLine($"Bulk send -> first id {result.MessageId}, {result.RecipientIds?.Length ?? 0} recipients");
}

if (mock.Supports(SmsCapabilities.OtpSend))
{
    var otp = await mock.SendOtpAsync(
        recipient: "09121234567",
        request: new OtpRequest
        {
            Code = "48291",
            TemplateId = "LoginTemplate",
        },
        cancellationToken: cancellationToken);
    Console.WriteLine($"OTP send -> {otp.MessageId} (cost: {otp.Cost})");
}

// 3. Delivery status lookup
if (mock is ISmsDeliveryReporter reporter)
{
    var status = await reporter.GetMessageStatusAsync(
        new MessageIdentifier(single.MessageId, MessageIdentifierType.ProviderMessageId),
        cancellationToken);
    Console.WriteLine($"Status of {single.MessageId} -> {status.State} (raw: {status.RawStatus})");
}

// 4. Inspect what the Mock provider recorded
Console.WriteLine("Recorded messages:");
foreach (var message in mock.Messages)
{
    Console.WriteLine($"  [{message.Id}] -> {message.Recipient}: \"{message.MessageText}\" ({message.State})");
}
