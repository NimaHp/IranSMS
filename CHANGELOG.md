# تغییرات (Changelog)

**فارسی** | [English](CHANGELOG.en.md)

این سند از [Keep a Changelog](https://keepachangelog.com/fa/1.1.0/) و سیستم نسخه‌گذاری [SemVer](https://semver.org/lang/fa/) پیروی می‌کند.

## [Unreleased]

* افزوده شد — اطلاعات حساب (`ISmsAccountInfo`): `GetBalanceAsync` و `GetSenderLinesAsync` با `AccountBalanceResult` برای کاوه‌نگار، قاصدک، SMS.ir و ملی‌پیامک + Mock، مطابق مستندات رسمی هر سرویس؛ فعال با پرچم‌های `SmsCapabilities.AccountInfo` و `LineManagement`.

## 0.1.0-beta.2 — 2026-09-07

* رفع باگ Mock README: اصلاح نمونه‌کد `SentMessages`/`msg.Text` به `Messages`/`MessageText`
* سخت‌سازی `GhasedakEnvelope.Deserialize`: بدنهٔ JSON معیوب اکنون `IranSmsException` می‌دهد (نه `JsonException` خام) و `StatusCode` به‌صورت رشته هم خوانده می‌شود
* اعتبارسنجی کاوه‌نگار: افزودن `null/whitespace` guard برای `SendAsync`/`SendBulkAsync`/`SendOtpAsync` (همسان قاصدک/SMS.ir)
* چرخهٔ عمر `HttpClient`: هر ۴ Transport و کلاینت (`Kavenegar/Ghasedak/SmsIr/Melipayamak`) اکنون `IDisposable` با مالکیت `_ownsHttp` — وقتی `HttpClient` بیرونی ندادی، `Dispose()` آن را آزاد می‌کند
* سقف bulk در Mock: اعمال `MaxBulkRecipients=200` برای parity با کاوه‌نگار
* سخت‌گیری `OtpRequest.SendDate`: هر ۵ کلاینت (۴ واقعی + Mock) در صورت مقداردهی `SendDate`، `NotSupportedException` صریح پرتاب می‌کنند (فعلاً هیچ Provider زمان‌بندی را اجرا نمی‌کند)
* بهینه‌سازی کوچک: حذف `System.Linq` از Kavenegar bulk و کش ضمنی `JsonSerializerOptions` در SmsIr
* CI/CD: افزودن `concurrency`، کش `NuGet` و `dotnet format --verify-no-changes` به `build.yml`؛ اصلاح `sed` شکنندهٔ `release.yml` و `embedded PDB` در `Directory.Build.props`

## 0.1.0-beta.1 — 2026-08-22

نخستین نسخهٔ بتا — یکپارچه‌سازی سامانه‌های پیامکی ایران با رابط ISmsClient:

  * هسته: تعریف قراردادهای اصلی (ISmsClient، ISmsBulkSender، ISmsOtpSender و ISmsDeliveryReporter)، پرچم‌های قابلیت (SmsCapabilities) و مدیریت استثناها (IranSmsException)

  * ارائه‌دهندگان: پشتیبانی کامل از کاوه‌نگار، قاصدک، SMS.ir، ملی‌پیامک و کلاینت Mock (همگی با پشتیبانی از Send، BulkSend، OtpSend و DeliveryStatus)

  * تزریق وابستگی (DI): ارائهٔ پکیج مستقل IranSms.DependencyInjection با قابلیت ثبت هوشمند بر اساس ویژگی‌ها و مدیریت مستقیم کلاینت توسط مصرف‌کننده از طریق AddIranSms

  * پروژه‌های نمونه: نمونهٔ Basic (کنسول بدون DI)، نمونهٔ AspNetCore (به‌صورت Minimal API) و نمونهٔ MultiProvider (مسیریابی پویا بر اساس قابلیت‌ها)

  * تست‌ها: دارای ۱۵۰ تست واحد همراه با پیاده‌سازی FakeTransport اختصاصی برای هر ارائه‌دهنده
