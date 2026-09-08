# Changelog

**English** | [فارسی](CHANGELOG.md)

This project adheres to [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
