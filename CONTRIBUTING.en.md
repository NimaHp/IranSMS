# Contributing Guide

**English** | [فارسی](CONTRIBUTING.md)

Thank you for contributing to IranSMS! This guide will help you get started with setting up the project, running tests, and submitting your changes.

## Prerequisites

* .NET 10 SDK

## Project Setup

Clone the repository and build the solution:

```bash
git clone <repository-url>
dotnet restore IranSMS.slnx
dotnet build IranSMS.slnx -c Release
```

## Running Tests

Execute the unit test suite:

```bash
./tests/IranSMS.Tests/bin/Release/net10.0/IranSMS.Tests.exe
# or
dotnet build IranSMS.slnx -c Release
```

* All tests must pass cleanly (150 tests with `xunit v3` + `FluentAssertions`).
* The `Mock` provider with deterministic `mock-{n}` identifiers is available for local testing.

## Coding Conventions

* **Strict warnings policy:** Controlled via `.editorconfig` + `TreatWarningsAsErrors`. Builds with warnings will not be accepted.
* **Modern .NET conventions:** `Nullable` and `ImplicitUsings` are enabled project-wide; `LangVersion` is `latest`.
* **`netstandard2.0` compatibility:** Core and all providers target `netstandard2.0` — do not use `net8`/`net10`-only APIs in `src/`; they are allowed in tests and samples.
* **`HttpClient` ownership:** The lifetime of `HttpClient` is consumer-owned; never create a global `HttpClient` or `IHttpClientFactory` inside `src/` — the instance is injected from outside.
* **Capability-aware registration:** Registration in `IranSms.DependencyInjection` must be capability-aware; register only the interfaces the instance actually implements — avoid blind casts.
* **No fabricated identifiers:** When a provider returns no `MessageId`, do not generate a synthetic one; throw `IranSmsException` with `RawResponseBody` instead.
* **Error mapping:** Provider errors must map to `IranSmsException` with `ProviderName` and `ProviderStatusCode`; `RawResponseBody` is for secure diagnostics only.
* **Input bounds:** Per-operator limits (bulk cap, message length, `senderLine` requirement) must be validated before any network call.
* **No extra dependencies:** `Core` stays dependency-free; each provider depends only on `Core`.

## Bilingual Documentation

Persian is the primary documentation language (files without a language suffix). English documentation files use the `*.en.md` suffix.

* Any documentation change must be reflected in **both language versions**.
* Do not remove English documentation files.
* Preserve the language-switching header at the top of every documentation file.

## Pull Request Checklist

Before opening a pull request, ensure the following steps are complete:

* [ ] Build and test suites pass with zero warnings
* [ ] `CHANGELOG.md` is updated under the Unreleased section (in both languages)
* [ ] Documentation is updated across both language versions where appropriate
* [ ] Tests with `FakeTransport` have been added for `HttpClient` or error-mapping changes
