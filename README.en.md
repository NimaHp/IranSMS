# IranSMS

**Unified Iranian SMS Abstraction for `.NET`** — Version `0.1.0-beta.1`

[![Build](https://github.com/NimaHp/IranSMS/actions/workflows/build.yml/badge.svg)](https://github.com/NimaHp/IranSMS/actions)
[![License](https://img.shields.io/github/license/NimaHp/IranSMS)](LICENSE)
[![NuGet version](https://img.shields.io/nuget/v/IranSms.Core)](https://www.nuget.org/packages/IranSms.Core)
[![NuGet downloads](https://img.shields.io/nuget/dt/IranSms.Core)](https://www.nuget.org/packages/IranSms.Core)
[![Release](https://img.shields.io/github/v/release/NimaHp/IranSMS)](https://github.com/NimaHp/IranSMS/releases)
[![Last commit](https://img.shields.io/github/last-commit/NimaHp/IranSMS)](https://github.com/NimaHp/IranSMS)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0-512BD4)](src/IranSms.Core)

[فارسی](README.md) | **English**

---

## About

A lightweight, dependency-free, and provider-agnostic library for sending SMS via Iranian operators in `.NET` — featuring a unified contract (`ISmsClient`), discoverable capabilities (`SmsCapabilities`), and capability-aware Dependency Injection (DI) support without vendor lock-in.

### Supported Providers

| Package | Client | Capabilities | Extra Dependency | Notes |
|---|---|---|---|---|
| IranSms.Core | ISmsClient | Base contracts & capability interfaces | — | Zero dependencies |
| IranSms.Providers.Kavenegar | KavenegarClient | Single · Bulk · OTP · Delivery status | System.Text.Json | Bulk up to 200 recipients |
| IranSms.Providers.Ghasedak | GhasedakClient | Single · Bulk · OTP · Delivery status | System.Text.Json | Bulk up to 100; max 1000 chars |
| IranSms.Providers.SmsIr | SmsIrClient | Single · Bulk · OTP · Delivery status | System.Text.Json | Bulk up to 100; numeric senderLine required |
| IranSms.Providers.Melipayamak | MelipayamakClient | Single · Bulk · OTP · Delivery status | — | senderLine required |
| IranSms.Providers.Mock | MockSmsClient | Single · Bulk · OTP · Delivery status | — | In-memory with deterministic mock-{n} IDs |
| IranSms.DependencyInjection | AddIranSms | Capability-aware registration | DI.Abstractions | Depends strictly on Core |

### Architecture

```
              IranSms.Core  (netstandard2.0 — zero dependencies)
                 ↑         ↑
                 │         │
      IranSms.DependencyInjection   IranSms.Providers.*  (each depends on Core only)
                 ↑         │
                 └──── Consumer ──── new KavenegarClient(apiKey)
```

* Each provider depends solely on Core; installing Kavenegar pulls no `Microsoft.Extensions.*` transitive dependencies.
* `IranSms.DependencyInjection` depends strictly on Core and `Microsoft.Extensions.DependencyInjection.Abstractions`.
* Client instantiation and `HttpClient` lifecycle management are consumer-owned and registered via `AddIranSms`.

### Capabilities

| Capability | Flag | Interface | Status |
|---|---|---|---|
| Single send | Send | ISmsClient.SendAsync | ✅ Implemented |
| Bulk send | BulkSend | ISmsBulkSender.SendBulkAsync | ✅ Implemented |
| OTP / Templated send | OtpSend | ISmsOtpSender.SendOtpAsync | ✅ Implemented |
| Delivery status | DeliveryStatus | ISmsDeliveryReporter.GetMessageStatusAsync | ✅ Implemented |
| Heterogeneous send | HeterogeneousSend | — | 🗓 Roadmap |
| Scheduled send | ScheduledSend | — | 🗓 Roadmap |
| Message history | MessageHistory | — | 🗓 Roadmap |
| Receive message | Receive | — | 🗓 Roadmap |
| Account info | AccountInfo | — | 🗓 Roadmap |
| Line management | LineManagement | — | 🗓 Roadmap |
| Template management | TemplateManagement | — | 🗓 Roadmap |
| Flash message | FlashMessage | — | 🗓 Roadmap |
| Voice message | VoiceMessage | — | 🗓 Roadmap |
| OTP template inspection | OtpTemplateInspection | — | 🗓 Roadmap |

Check capabilities without `HasFlag` (to avoid boxing overhead on `netstandard2.0`):

```csharp
if ((client.Capabilities & SmsCapabilities.OtpSend) == SmsCapabilities.OtpSend) { ... }
// or
if (client.Supports(SmsCapabilities.OtpSend)) { ... }
// or
if (client is ISmsOtpSender otp) { ... }
```

### Key Features

* ✅ **Zero Transitive Dependencies:** Core is completely dependency-free; provider packages depend only on Core.
* ✅ **Consumer-Owned Lifecycle:** You control `HttpClient` creation and API key management.
* ✅ **Capability-Aware DI:** Registers only the interfaces that the underlying client instance actually implements.
* ✅ **Highly Testable:** Includes a Mock provider with deterministic `mock-{n}` identifiers.
* ✅ **HttpClient ownership:** The client lifecycle is fully owned by the consumer. If no `HttpClient` is injected, the default 100-second `HttpClient.Timeout` applies — pass your own pre-configured `HttpClient` to customize timeouts, handlers, and policies.
* ✅ **netstandard2.0 Compatible:** Works across all modern .NET runtimes (from .NET Framework to .NET 10).
* ✅ **Strict Error Mapping:** Throws clear exceptions instead of returning dummy values when `MessageId` is missing.

## Installation

```bash
dotnet add package IranSms.Core
dotnet add package IranSms.Providers.Kavenegar   # or Ghasedak / SmsIr / Melipayamak
dotnet add package IranSms.Providers.Mock        # for local testing
dotnet add package IranSms.DependencyInjection   # optional — only if using DI
```

## Quick Start

### Basic Usage (Without DI)

```csharp
using IranSms.Providers.Kavenegar;

var client = new KavenegarClient(Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY")!);

var result = await client.SendAsync("09121234567", "Hello from IranSMS!");
Console.WriteLine(result.MessageId);

if (client is ISmsOtpSender otp)
{
    var r = await otp.SendOtpAsync("09121234567", new OtpRequest
    {
        Code = "48291",
        TemplateId = "LoginTemplate",
    });
}

if (client is ISmsDeliveryReporter reporter)
{
    var status = await reporter.GetMessageStatusAsync(
        new MessageIdentifier(result.MessageId, MessageIdentifierType.ProviderMessageId));
    Console.WriteLine(status.State);
}
```

### ASP.NET Core Integration (With DI)

```csharp
using IranSms.DependencyInjection;
using IranSms.Providers.Kavenegar;

builder.Services.AddIranSms(new KavenegarClient(
    builder.Configuration["Kavenegar:ApiKey"]!));

app.MapPost("/sms/send", async (SendRequest req, ISmsClient sms, CancellationToken ct) =>
{
    var r = await sms.SendAsync(req.Recipient, req.Message, req.SenderLine, ct);
    return Results.Ok(new { r.MessageId });
});
```

> **Note:** Never hardcode API keys in source code. Retrieve them from `IConfiguration`, `UserSecrets`, or Key Vault.

### Multi-Provider Dispatch

```csharp
services.AddIranSms(new MockSmsClient("Mock"));
services.AddIranSms(new KavenegarClient(kavenegarKey));
services.AddIranSms(new GhasedakClient(ghasedakKey));

var clients = provider.GetServices<ISmsClient>();
var otpSender = clients.FirstOrDefault(c => c.Supports(SmsCapabilities.OtpSend)) as ISmsOtpSender;
```

## Error Handling

```csharp
try
{
    var r = await client.SendAsync(recipient, message);
}
catch (IranSmsException ex) when (ex.ProviderName == "Ghasedak")
{
    // ex.ProviderStatusCode and ex.RawResponseBody are populated
    // Do not log RawResponseBody to public sinks — it may contain sensitive message content or phone numbers
    logger.LogWarning("Ghasedak error {Code}: {Message}", ex.ProviderStatusCode, ex.Message);
}
catch (HttpRequestException)
{
    // Network error or timeout — RawResponseBody is empty
}
```

## Sample Projects

| Sample | Description | Run |
|---|---|---|
| samples/Basic | Console app without DI demonstrating all capabilities via Mock | dotnet run --project samples/Basic |
| samples/AspNetCore | Minimal API integration using AddIranSms | dotnet run --project samples/AspNetCore |
| samples/MultiProvider | Registers 5 providers with capability-aware routing | dotnet run --project samples/MultiProvider |

Set API keys via environment variables:

```bash
export KAVENEGAR_API_KEY=...
export GHASEDAK_API_KEY=...
export SMSIR_API_KEY=...
export MELIPAYAMAK_USERNAME=... MELIPAYAMAK_PASSWORD=...
```

## Project Packages

| Package | Status | Usage |
|---|---|---|
| IranSms.Core | ✅ | Core contracts and capabilities — zero dependencies |
| IranSms.Providers.Kavenegar | ✅ | Kavenegar SMS provider implementation |
| IranSms.Providers.Ghasedak | ✅ | Ghasedak SMS provider implementation |
| IranSms.Providers.SmsIr | ✅ | SMS.ir provider implementation |
| IranSms.Providers.Melipayamak | ✅ | Melipayamak SMS provider implementation |
| IranSms.Providers.Mock | ✅ | In-memory implementation for testing |
| IranSms.DependencyInjection | ✅ | Capability-aware DI registration |

## Tests & Coverage

Windows (PowerShell):

```powershell
dotnet build -c Release
dotnet run --project tests\IranSms.Tests -c Release --no-build --framework net10.0
dotnet run --project tests\IranSms.Tests -c Release --no-build --framework net8.0
```

Linux / macOS (bash):

```bash
dotnet build -c Release
dotnet run --project tests/IranSms.Tests -c Release --no-build --framework net10.0
dotnet run --project tests/IranSms.Tests -c Release --no-build --framework net8.0
```

150 test cases (`xunit v3` + `FluentAssertions`) — all passing on both .NET 10 and .NET 8.

## CI/CD Pipeline

* **build.yml Workflow:** Builds and executes test suites on every push and `pull_request`.
* **release.yml Workflow:** Automatically publishes packages to `NuGet.org` and creates a `GitHub Release` on `v*` tags.

## License

Released under the [MIT License](LICENSE).
