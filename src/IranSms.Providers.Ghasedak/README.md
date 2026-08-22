# IranSms.Providers.Ghasedak

[Ghasedak](https://ghasedak.me) provider for [IranSMS](https://github.com/NimaHp/IranSMS).

## Installation

```
dotnet add package IranSms.Providers.Ghasedak
```

Requires `IranSms.Core`.

## Usage

```csharp
using IranSms.Providers.Ghasedak;

var client = new GhasedakClient(Environment.GetEnvironmentVariable("GHASEDAK_API_KEY")!);

// Single send — message up to 1000 characters
var result = await client.SendAsync("09121234567", "Hello from IranSMS!");

// Bulk — up to 100 recipients
var bulk = await client.SendBulkAsync(new[] { "09121234567", "09351111111" }, "Hello!");

// OTP / templated — TemplateId is the Ghasedak template name
var otp = await client.SendOtpAsync("09121234567", new OtpRequest
{
    Code = "48291",
    TemplateId = "MyTemplate",
});

// Delivery status
var status = await client.GetMessageStatusAsync(
    new MessageIdentifier(result.MessageId, MessageIdentifierType.ProviderMessageId));
```

For DI, see `IranSms.DependencyInjection`:

```csharp
services.AddIranSms(new GhasedakClient(apiKey));
```

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
