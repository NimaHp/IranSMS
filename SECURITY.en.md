# Security Policy

**English** | [فارسی](SECURITY.md)

## Supported Versions

| Version | Supported |
| :--- | :--- |
| 0.1.x-beta | ✅ |

## Reporting Vulnerabilities

To report a security vulnerability, please use **GitHub Private Vulnerability Reporting** (accessible via the "Security" tab of this repository). Please refrain from reporting security issues in public issues or pull requests.

Include the following details in your report:

* Affected package name and version
* Steps to reproduce the vulnerability
* Potential impact assessment

Target initial response time: within 7 business days.

## Security & Design Architecture

* **Credential handling:** `ApiKey` / username and password are never hard-coded and are held as private readonly `string` fields; load them from `IConfiguration`, `UserSecrets`, or Key Vault via environment variables.
* **No raw-response leakage:** `IranSmsException.RawResponseBody` may contain message text or recipient numbers — never write it to public logs, HTTP responses, or long-term storage; log only `ProviderName` and `ProviderStatusCode`.
* **No fabricated identifiers:** When a provider returns no `MessageId`, no synthetic identifier is generated — an `IranSmsException` with `RawResponseBody` is thrown instead.
* **`HttpClient` ownership:** The lifetime of `HttpClient` and `HttpMessageHandler` is fully consumer-owned; the library creates no global `HttpClient`.
* **HTTP timeout:** When no `HttpClient` is injected, the default `HttpClient` timeout (100 seconds) applies; inject a dedicated `HttpClient` with a custom `Timeout` for precise control.
* **Input validation bounds:** Each client enforces operator limits (e.g. bulk recipient cap, message length) before any network call and throws `ArgumentException` on violation.
* **Stateless & thread-safe:** Clients hold no shared mutable state and are safe for concurrent invocation.
* **Minimal dependencies:** Core has zero external dependencies and each provider depends only on `Core`.
* **Data privacy:** This library stores no personal data and performs only send and status-query operations.
