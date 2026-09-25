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

## Error handling

Every provider behaves the same way, so callers can branch once instead of per provider.

| Situation | Result |
| --- | --- |
| Invalid input (null/empty recipient, blank message, blank sender line or client reference) | `ArgumentNullException` / `ArgumentException` from `SmsValidation`, before any network call |
| Capability gap (for example `OtpRequest.SendDate` without `SmsCapabilities.ScheduledSend`) | `NotSupportedException` |
| Provider rejection, throttling, malformed answer | `IranSmsException` with `Kind` (`SmsErrorKind`), `ProviderName`, `ProviderStatusCode`, `Operation` |
| Transport/timeout failure | `HttpRequestException` / `TaskCanceledException` from the transport |

```csharp
try
{
    await client.SendAsync("09121234567", "hello");
}
catch (IranSmsException ex) when (ex.IsTransient)
{
    // RateLimited / Transport / Timeout / ProviderUnavailable only.
}
```

`SmsErrorKind.Cancelled` is never treated as transient: replaying a send without an idempotency key can duplicate messages.

Every provider fills `Kind` and `Operation`, so failures are comparable across providers:

```csharp
catch (IranSmsException ex)
{
    if (ex.Kind == SmsErrorKind.InsufficientBalance) { /* top up the account */ }
    log(ex.ProviderName, ex.Operation, ex.Kind, ex.ProviderStatusCode);
}
```

| HTTP status reported by the provider | `SmsErrorKind` |
| --- | --- |
| 401, 403 | `Unauthorized` |
| 402 | `InsufficientBalance` |
| 408, 504 | `Timeout` |
| 429 | `RateLimited` |
| 5xx | `ProviderUnavailable` (transient) |
| other 4xx | `ProviderRejected` |

`Operation` holds the provider method/endpoint that failed (`sms/send`, `SendSingleSMS`, `send/bulk`, `account/info`, …) and never contains recipients, message text or credentials. Envelopes that cannot be parsed, or successful responses missing a message id, are reported as `MalformedResponse`.

## Input validation

`SmsValidation` normalizes inputs identically for every provider: trims, transliterates Persian (`۰-۹`) and Arabic-Indic (`٠-٩`) digits to ASCII, and strips spacing/formatting characters. Country prefixes are preserved exactly as written (`+98…`, `0098…`, `09…`). All five clients (Kavenegar, Ghasedak, SMS.ir, Melipayamak, Mock) run it before every send, so `SendAsync("۰۹۱۲ ۰۰۰۰۰۰۰", …)` and `SendAsync("09120000000", …)` reach the provider as the same string. Blank messages and whitespace-only sender lines are rejected before any network call.

## Batch results

`SmsSendItemResult` is one recipient outcome; `SmsBulkSendResult` aggregates a batch and exposes `Items`, `SucceededCount`, `FailedCount`, `AllSucceeded`, `IsPartialFailure` and `AllFailed`. A rejected recipient never fails the whole batch.

## Delivery status

Providers map their own status codes onto `MessageDeliveryState`. `MessageDeliveryStateExtensions` gives one vocabulary: `IsFinal()`, `IsSuccessful()`, `IsFailure()`, `IsPending()`.

## Client references

`MessageIdentifier.ForProviderMessageId(...)` and `MessageIdentifier.ForClientReferenceId(...)` build identifiers for status lookups. `OtpRequest.ClientReferenceId` lets you attach a client reference so providers with local-reference support can de-duplicate retries.

## License

MIT License — see the repository [LICENSE](https://github.com/NimaHp/IranSMS/blob/main/LICENSE).
