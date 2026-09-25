# Changelog

**English** | [فارسی](CHANGELOG.md)

This project adheres to [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

* Normalized errors: added `SmsErrorKind`, `SmsErrorKindExtensions.IsTransient`, and `IranSmsException.Kind`/`Operation`/`IsTransient` plus the `ProviderRejected`, `RateLimited` and `MalformedResponse` factories.
* Provider error classification: all 4 transports and 4 clients now set `Kind` and `Operation` on every failure; HTTP codes are mapped through `SmsErrorKindExtensions.FromHttpStatus` (401/403 → `Unauthorized`, 402 → `InsufficientBalance`, 408/504 → `Timeout`, 429 → `RateLimited`, 5xx → transient `ProviderUnavailable`) and unparseable answers are reported as `MalformedResponse`.
* Consistent validation: added `SmsValidation` with `EnsureRecipient`, `NormalizeRecipient` (Persian/Arabic digit transliteration), `EnsureMessage`, `EnsureSenderLine` and `EnsureClientReferenceId`.
* Wired `SmsValidation` into all 5 clients (Kavenegar, Ghasedak, SMS.ir, Melipayamak and Mock): numbers are normalized identically before sending (Persian/Arabic digits to ASCII, spacing/dashes/parentheses removed), blank messages and whitespace-only sender lines are rejected, sender lines are trimmed, and Mock now echoes `OtpSendResult.ClientReferenceId`.
* Per-recipient batch results: added `SmsSendItemResult` and `SmsBulkSendResult` with success/failure counters and `IsPartialFailure`, `AllSucceeded`, `AllFailed` flags.
* Delivery state classification: added `MessageDeliveryStateExtensions` with `IsFinal`, `IsSuccessful`, `IsFailure` and `IsPending`.
* Client references: added `OtpRequest.ClientReferenceId`, `OtpSendResult.ClientReferenceId` and the `MessageIdentifier.ForProviderMessageId`/`ForClientReferenceId` factories.
* Documented the behaviour of these features in `IranSms.Core/README.md`; all changes are additive with no public API break.
* Increased the test suite to 273 tests; build, net8/net10 tests and formatting completed successfully.

## 0.2.0-beta.2 — 2026-09-24

* Fixed official provider response handling for Ghasedak, Melipayamak, SMS.ir, and Kavenegar, including partial failures and invalid input validation.
* Hardened transports with controlled timeout and redirect behavior, bounded response sizes, redacted exception messages, and transport tests.
* Protected the ASP.NET Core sample with an API key, rate limiting, and generic production error handling; added `SenderLines` with a compatibility alias.
* Enabled `packages.lock.json`, `RepositoryCommit`, SourceLink, and GitHub Packages publishing through `release.yml`.
* Increased the test suite to 222 tests; build, net8/net10 tests, formatting, and package validation completed successfully.

## 0.2.0-beta.1 — 2026-09-09

* Added — Account info (`ISmsAccountInfo`): `GetBalanceAsync` and `GetSenderLinesAsync` with `AccountBalanceResult` for Kavenegar, Ghasedak, SMS.ir and Melipayamak + Mock, per each provider's official docs; enabled by `SmsCapabilities.AccountInfo` and `LineManagement`.
* Samples: surfaced `AccountInfo` in all 3 samples — `Basic` (console, `ISmsAccountInfo` + `GetBalance`/`GetSenderLines`), `AspNetCore` (`GET /account/balance` and `/account/lines`) and `MultiProvider` (`AccountInfo` capability dispatch).

## 0.1.0-beta.2 — 2026-09-07

* Fixed Mock README sample: `SentMessages`/`msg.Text` → `Messages`/`MessageText`
* Hardened `GhasedakEnvelope.Deserialize`: malformed JSON now surfaces as `IranSmsException` (not raw `JsonException`); `StatusCode` also accepts string numbers
* Validated Kavenegar inputs: added `null/whitespace` guards to `SendAsync`/`SendBulkAsync`/`SendOtpAsync` for parity with other providers
* HttpClient lifetime: all 4 transports and clients (`Kavenegar/Ghasedak/SmsIr/Melipayamak`) now implement `IDisposable` with `_ownsHttp` ownership — disposing the client releases the internally owned `HttpClient`
* Mock bulk cap: enforced `MaxBulkRecipients=200` for parity with Kavenegar
* Strict `OtpRequest.SendDate`: all 5 clients throw `NotSupportedException` when `SendDate` is set — no provider honours scheduled sends yet
* Small perf: removed `System.Linq` allocation from Kavenegar bulk and tightened `SmsIr` JSON options
* CI/CD: added `concurrency`, NuGet cache, and `dotnet format --verify-no-changes` to `build.yml`; hardened `release.yml` sed and embedded PDB in `Directory.Build.props`

## 0.1.0-beta.1 — 2026-08-22

First beta — unified ISmsClient abstraction for Iranian SMS providers:
 * Core — Base contracts (ISmsClient, ISmsBulkSender, ISmsOtpSender, ISmsDeliveryReporter), SmsCapabilities enum, and IranSmsException
 * Providers — Kavenegar, Ghasedak, SMS.ir, Melipayamak, and Mock (all supporting Send, BulkSend, OtpSend, and DeliveryStatus)
 * Dependency Injection — Provider-agnostic IranSms.DependencyInjection package, featuring consumer-owned lifecycle management and capability-aware registration via AddIranSms
 * Samples — Basic (standalone console app), AspNetCore (Minimal API), and MultiProvider (capability-based routing)
 * Testing — 150 unit tests featuring FakeTransport implementations for each provider
