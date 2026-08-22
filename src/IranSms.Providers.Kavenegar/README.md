# IranSms.Providers.Kavenegar

[Kavenegar](https://kavenegar.com) provider for [IranSMS](https://github.com/NimaHp/IranSMS).

## Installation

```
dotnet add package IranSms.Providers.Kavenegar
```

Requires `IranSms.Core`.

## Usage

```csharp
using IranSms.Providers.Kavenegar;

var client = new KavenegarClient(Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY")!);

// Single send
var result = await client.SendAsync("09121234567", "Hello from IranSMS!");

// Bulk — up to 200 recipients
var bulk = await client.SendBulkAsync(new[] { "09121234567", "09351111111" }, "Hello!");

// OTP / templated
var otp = await client.SendOtpAsync("09121234567", new OtpRequest
{
    Code = "48291",
    TemplateId = "LoginTemplate",
});

// Delivery status
var status = await client.GetMessageStatusAsync(
    new MessageIdentifier(result.MessageId, MessageIdentifierType.ProviderMessageId));
```

For DI, see `IranSms.DependencyInjection`:

```csharp
services.AddIranSms(new KavenegarClient(apiKey));
```

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
