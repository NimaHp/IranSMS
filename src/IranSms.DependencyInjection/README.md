# IranSms.DependencyInjection

Capability-aware DI registration for [IranSMS](https://github.com/NimaHp/IranSMS).

## Installation

```
dotnet add package IranSms.DependencyInjection
```

Requires `IranSms.Core`.

## Usage

```csharp
using IranSms.DependencyInjection;
using IranSms.Providers.Kavenegar;

builder.Services.AddIranSms(new KavenegarClient(
    builder.Configuration["Kavenegar:ApiKey"]!));

// Any provider works — the registration is provider-agnostic and capability-aware:
builder.Services.AddIranSms(new MockSmsClient("Mock"));

// Resolve — only the interfaces the instance actually implements are registered:
app.MapPost("/sms/send", async (SendRequest req, ISmsClient sms, CancellationToken ct) =>
{
    var r = await sms.SendAsync(req.Recipient, req.Message, req.SenderLine, ct);
    return Results.Ok(new { r.MessageId });
});

// Capability-specific resolution:
if (provider.GetService<ISmsOtpSender>() is { } otp)
    await otp.SendOtpAsync(recipient, new OtpRequest { Code = "48291", TemplateId = "T" });
```

Resolving an unimplemented capability (e.g. `ISmsBulkSender` on a send-only client) throws `InvalidOperationException`, not `InvalidCastException`.

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
