# IranSms.Providers.Melipayamak

[Melipayamak](https://melipayamak.com) provider for [IranSMS](https://github.com/NimaHp/IranSMS).

## Installation

```
dotnet add package IranSms.Providers.Melipayamak
```

Requires `IranSms.Core`.

## Usage

```csharp
using IranSms.Providers.Melipayamak;

var client = new MelipayamakClient(
    Environment.GetEnvironmentVariable("MELIPAYAMAK_USERNAME")!,
    Environment.GetEnvironmentVariable("MELIPAYAMAK_PASSWORD")!);

// Single send — senderLine is required
var result = await client.SendAsync("09121234567", "Hello from IranSMS!", senderLine: "50001234");

// Bulk
var bulk = await client.SendBulkAsync(new[] { "09121234567", "09351111111" }, "Hello!", "50001234");

// OTP — Code and senderLine are required
var otp = await client.SendOtpAsync("09121234567", new OtpRequest
{
    Code = "48291",
    SenderLine = "50001234",
});

// Delivery status
var status = await client.GetMessageStatusAsync(
    new MessageIdentifier(result.MessageId, MessageIdentifierType.ProviderMessageId));
```

For DI, see `IranSms.DependencyInjection`:

```csharp
services.AddIranSms(new MelipayamakClient(username, password));
```

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
