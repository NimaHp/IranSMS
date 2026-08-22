# IranSms.Core

The provider-agnostic core of [IranSMS](https://github.com/NimaHp/IranSMS) — unified contracts for Iranian SMS providers.

## Installation

```
dotnet add package IranSms.Core
```

## Usage

```csharp
using IranSms;

ISmsClient client = /* any provider */;

if (client.Supports(SmsCapabilities.OtpSend) && client is ISmsOtpSender otp)
{
    var result = await otp.SendOtpAsync("09121234567", new OtpRequest
    {
        Code = "48291",
        TemplateId = "LoginTemplate",
    });
    Console.WriteLine(result.MessageId);
}

var status = await (client as ISmsDeliveryReporter)!.GetMessageStatusAsync(
    new MessageIdentifier("msg-id", MessageIdentifierType.ProviderMessageId));
```

Check capabilities with `client.Supports(flag)` or `(client.Capabilities & flag) == flag` — do not use `HasFlag` on `netstandard2.0`.

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
