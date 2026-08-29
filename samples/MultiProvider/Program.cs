using IranSms;
using IranSms.DependencyInjection;
using IranSms.Providers.Ghasedak;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Melipayamak;
using IranSms.Providers.Mock;
using IranSms.Providers.SmsIr;
using Microsoft.Extensions.DependencyInjection;

// MultiProvider sample: register several provider clients in one DI container and
// dispatch to the first one that supports a capability. Real providers need
// credentials from environment variables; the Mock provider always works.
// Each client is built by the consumer (consumer-owned) and registered with the
// generic, provider-agnostic AddIranSms(...).

string? kavenegarKey = Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY");
string? ghasedakKey = Environment.GetEnvironmentVariable("GHASEDAK_API_KEY");
string? smsIrKey = Environment.GetEnvironmentVariable("SMSIR_API_KEY");
string? melipayamakUser = Environment.GetEnvironmentVariable("MELIPAYAMAK_USERNAME");
string? melipayamakPass = Environment.GetEnvironmentVariable("MELIPAYAMAK_PASSWORD");

var services = new ServiceCollection();
services.AddIranSms(new MockSmsClient("Mock"));
if (!string.IsNullOrWhiteSpace(kavenegarKey))
    services.AddIranSms(new KavenegarClient(kavenegarKey));
if (!string.IsNullOrWhiteSpace(smsIrKey))
    services.AddIranSms(new SmsIrClient(smsIrKey));
if (!string.IsNullOrWhiteSpace(melipayamakUser) && !string.IsNullOrWhiteSpace(melipayamakPass))
    services.AddIranSms(new MelipayamakClient(melipayamakUser, melipayamakPass));
if (!string.IsNullOrWhiteSpace(ghasedakKey))
    services.AddIranSms(new GhasedakClient(ghasedakKey));

var provider = services.BuildServiceProvider();

// Resolve every registered ISmsClient; the container keeps the registration order.
var clients = provider.GetRequiredService<IEnumerable<ISmsClient>>().ToList();
Console.WriteLine($"Registered providers: {string.Join(", ", clients.Select(c => c.ProviderName))}");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
var cancellationToken = cts.Token;

// Capability-aware dispatch: pick the first provider supporting OTP.
var otpSender = clients.FirstOrDefault(c => c.Supports(SmsCapabilities.OtpSend)) as ISmsOtpSender;
if (otpSender is not null)
{
    var otp = await otpSender.SendOtpAsync(
        recipient: "09121234567",
        request: new OtpRequest { Code = "48291", TemplateId = "LoginTemplate" },
        cancellationToken);
    Console.WriteLine($"OTP sent via {((ISmsClient)otpSender).ProviderName} -> {otp.MessageId}");
}
else
{
    Console.WriteLine("No registered provider supports OTP.");
}

// Capability-aware dispatch: pick the first provider supporting delivery status.
var reporter = clients.FirstOrDefault(c => c.Supports(SmsCapabilities.DeliveryStatus)) as ISmsDeliveryReporter;
if (reporter is not null)
{
    var status = await reporter.GetMessageStatusAsync(
        new MessageIdentifier("mock-1", MessageIdentifierType.ProviderMessageId),
        cancellationToken);
    Console.WriteLine($"Status via {((ISmsClient)reporter).ProviderName} -> {status.State} (raw: {status.RawStatus})");
}
