# IranSms.Providers.SmsIr

[SMS.ir](https://sms.ir) provider for [IranSMS](https://github.com/NimaHp/IranSMS).

## Installation

```
dotnet add package IranSms.Providers.SmsIr
```

Requires `IranSms.Core`.

## Usage

```csharp
using IranSms.Providers.SmsIr;

var client = new SmsIrClient(Environment.GetEnvironmentVariable("SMSIR_API_KEY")!);

// Single send — senderLine must be a numeric line number
var result = await client.SendAsync("09121234567", "Hello from IranSMS!", senderLine: "30001234");

// Bulk — up to 100 recipients
var bulk = await client.SendBulkAsync(new[] { "09121234567", "09351111111" }, "Hello!", "30001234");

// OTP / verify — TemplateId must be an integer string
var otp = await client.SendOtpAsync("09121234567", new OtpRequest
{
    Code = "48291",
    TemplateId = "123456",
});

// Delivery status
var status = await client.GetMessageStatusAsync(
    new MessageIdentifier(result.MessageId, MessageIdentifierType.ProviderMessageId));
```

For DI, see `IranSms.DependencyInjection`:

```csharp
services.AddIranSms(new SmsIrClient(apiKey));
```

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
