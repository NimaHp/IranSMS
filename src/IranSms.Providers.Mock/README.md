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
foreach (var msg in client.Messages)
    Console.WriteLine($"{msg.Recipient}: {msg.MessageText}");

// Account info — configured credit + sender lines
var balance = await client.GetBalanceAsync();
var lines = await client.GetSenderLinesAsync();
```

Supports all six implemented capabilities: `Send`, `BulkSend`, `OtpSend`, `DeliveryStatus`, `AccountInfo`, `LineManagement`.

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
