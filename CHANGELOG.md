# تغییرات (Changelog)

**فارسی** | [English](CHANGELOG.en.md)

این سند از [Keep a Changelog](https://keepachangelog.com/fa/1.1.0/) و سیستم نسخه‌گذاری [SemVer](https://semver.org/lang/fa/) پیروی می‌کند.

## [Unreleased]

* خطاهای نرمال‌شده: افزودن `SmsErrorKind` و `SmsErrorKindExtensions.IsTransient` و `IranSmsException.Kind`/`Operation`/`IsTransient` به‌همراه factoryهای `ProviderRejected`، `RateLimited` و `MalformedResponse`.
* اعتبارسنجی یکدست: افزودن `SmsValidation` با `EnsureRecipient`، `NormalizeRecipient` (ترنسلیتریشن ارقام فارسی/عربی)، `EnsureMessage`، `EnsureSenderLine` و `EnsureClientReferenceId`.
* نتیجهٔ هر گیرنده در ارسال گروهی: افزودن `SmsSendItemResult` و `SmsBulkSendResult` با شمارش موفق/ناموفق و تشخیص `IsPartialFailure`، `AllSucceeded` و `AllFailed`.
* دسته‌بندی وضعیت تحویل: افزودن `MessageDeliveryStateExtensions` با `IsFinal`، `IsSuccessful`، `IsFailure` و `IsPending`.
* ارجاع سمت کلاینت: افزودن `OtpRequest.ClientReferenceId`، `OtpSendResult.ClientReferenceId` و factoryهای `MessageIdentifier.ForProviderMessageId`/`ForClientReferenceId`.
* مستندسازی رفتار این قابلیت‌ها در `IranSms.Core/README.md`؛ همهٔ تغییها additive و بدون شکستن API عمومی.
* تعداد تست‌ها به ۲۷۳ رسید؛ build، تست net8/net10 و formatter با موفقیت اجرا شدند.

## 0.2.0-beta.2 — 2026-09-24

* اصلاح پاسخ‌های رسمی providerها در Ghasedak، Melipayamak، SMS.ir و Kavenegar و افزودن validation برای partial failure و ورودی‌های نامعتبر.
* سخت‌سازی transportها با timeout و redirect کنترل‌شده، محدودیت اندازهٔ پاسخ، حذف raw body از پیام exception و تست‌های transport.
* محافظت از نمونهٔ ASP.NET Core با API key، rate limit و exception handling عمومی؛ افزودن قابلیت `SenderLines` با حفظ alias سازگاری.
* فعال‌سازی `packages.lock.json`، `RepositoryCommit`، SourceLink و انتشار packageها در GitHub Packages از طریق `release.yml`.
* تعداد تست‌ها به ۲۲۲ رسید؛ build، تست net8/net10، formatter و package validation با موفقیت اجرا شدند.

## 0.2.0-beta.1 — 2026-09-09

* افزوده شد — اطلاعات حساب (`ISmsAccountInfo`): `GetBalanceAsync` و `GetSenderLinesAsync` با `AccountBalanceResult` برای کاوه‌نگار، قاصدک، SMS.ir و ملی‌پیامک + Mock، مطابق مستندات رسمی هر سرویس؛ فعال با پرچم‌های `SmsCapabilities.AccountInfo` و `LineManagement`.
* نمونه‌ها: نمایش `AccountInfo` در هر ۳ نمونه — `Basic` (کنسول، `ISmsAccountInfo` + `GetBalance`/`GetSenderLines`)، `AspNetCore` (`GET /account/balance` و `/account/lines`) و `MultiProvider` (dispatch قابلیت `AccountInfo`).

## 0.1.0-beta.2 — 2026-09-07

* رفع باگ Mock README: اصلاح نمونه‌کد `SentMessages`/`msg.Text` به `Messages`/`MessageText`
* سخت‌سازی `GhasedakEnvelope.Deserialize`: بدنه JSON معیوب اکنون `IranSmsException` می‌دهد (نه `JsonException` خام) و `StatusCode` به‌صورت رشته هم خوانده می‌شود
* اعتبارسنجی کاوه‌نگار: افزودن `null/whitespace` guard برای `SendAsync`/`SendBulkAsync`/`SendOtpAsync` (همسان قاصدک/SMS.ir)
* چرخه عمر `HttpClient`: هر ۴ Transport و کلاینت (`Kavenegar/Ghasedak/SmsIr/Melipayamak`) اکنون `IDisposable` با مالکیت `_ownsHttp` — وقتی `HttpClient` بیرونی ندادی، `Dispose()` آن را آزاد می‌کند
* سقف bulk در Mock: اعمال `MaxBulkRecipients=200` برای parity با کاوه‌نگار
* سخت‌گیری `OtpRequest.SendDate`: هر ۵ کلاینت (۴ واقعی + Mock) در صورت مقداردهی `SendDate`، `NotSupportedException` صریح پرتاب می‌کنند (فعلاً هیچ Provider زمان‌بندی را اجرا نمی‌کند)
* بهینه‌سازی کوچک: حذف `System.Linq` از Kavenegar bulk و کش ضمنی `JsonSerializerOptions` در SmsIr
* CI/CD: افزودن `concurrency`، کش `NuGet` و `dotnet format --verify-no-changes` به `build.yml`؛ اصلاح `sed` شکننده `release.yml` و `embedded PDB` در `Directory.Build.props`

## 0.1.0-beta.1 — 2026-08-22

نخستین نسخه بتا — یکپارچه‌سازی سامانه‌های پیامکی ایران با رابط ISmsClient:

  * هسته: تعریف قراردادهای اصلی (ISmsClient، ISmsBulkSender، ISmsOtpSender و ISmsDeliveryReporter)، پرچم‌های قابلیت (SmsCapabilities) و مدیریت استثناها (IranSmsException)

  * ارائه‌دهندگان: پشتیبانی کامل از کاوه‌نگار، قاصدک، SMS.ir، ملی‌پیامک و کلاینت Mock (همگی با پشتیبانی از Send، BulkSend، OtpSend و DeliveryStatus)

  * تزریق وابستگی (DI): ارائه پکیج مستقل IranSms.DependencyInjection با قابلیت ثبت هوشمند بر اساس ویژگی‌ها و مدیریت مستقیم کلاینت توسط مصرف‌کننده از طریق AddIranSms

  * پروژه‌های نمونه: نمونه Basic (کنسول بدون DI)، نمونه AspNetCore (به‌صورت Minimal API) و نمونه MultiProvider (مسیریابی پویا بر اساس قابلیت‌ها)

  * تست‌ها: دارای ۱۵۰ تست واحد همراه با پیاده‌سازی FakeTransport اختصاصی برای هر ارائه‌دهنده
