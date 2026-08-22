# IranSms.Providers.Mock

In-memory mock provider for [IranSMS](https://github.com/NimaHp/IranSMS) — for local development and tests.

## Installation

```
dotnet add package IranSms.Providers.Mock
```

Requires `IranSms.Core`.

## Usage

```csharp
using IranSms.Providers.Mock;

var client = new MockSmsClient("MyMock");

// No network — deterministic mock-{n} identifiers
var result = await client.SendAsync("09121234567", "Hello!");
Console.WriteLine(result.MessageId); // mock-1

var bulk = await client.SendBulkAsync(new[] { "09121234567", "09351111111" }, "Hello!");
var otp = await client.SendOtpAsync("09121234567", new OtpRequest { Code = "48291" });
var status = await client.GetMessageStatusAsync(
    new MessageIdentifier(result.MessageId, MessageIdentifierType.ProviderMessageId));

// Inspect recorded messages
foreach (var msg in client.SentMessages)
    Console.WriteLine($"{msg.Recipient}: {msg.Text}");
```

Supports all four implemented capabilities: `Send`, `BulkSend`, `OtpSend`, `DeliveryStatus`.

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
