using IranSms;
using IranSms.Providers.Ghasedak;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Melipayamak;
using IranSms.Providers.Mock;
using IranSms.Providers.SmsIr;
using Microsoft.Extensions.DependencyInjection;

// MultiProvider sample: register several providers in one DI container and
// dispatch to the first one that supports a capability. Real providers need
// credentials from environment variables; the Mock provider always works.

const string apiKey = "YOUR_API_KEY_HERE";

var services = new ServiceCollection();
services.AddMock(options => options.ProviderName = "Mock");
services.AddKavenegar(options => options.ApiKey = apiKey);
services.AddSmsIr(options => options.ApiKey = apiKey);
services.AddMelipayamak(options =>
{
    options.Username = Environment.GetEnvironmentVariable("MELIPAYAMAK_USERNAME") ?? "YOUR_USERNAME";
    options.Password = Environment.GetEnvironmentVariable("MELIPAYAMAK_PASSWORD") ?? "YOUR_PASSWORD";
});
services.AddGhasedak(options => options.ApiKey = apiKey);

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
