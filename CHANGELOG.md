# تغییرات (Changelog)

**فارسی** | [English](CHANGELOG.en.md)

این سند از [Keep a Changelog](https://keepachangelog.com/fa/1.1.0/) و سیستم نسخه‌گذاری [SemVer](https://semver.org/lang/fa/) پیروی می‌کند.

## [Unreleased]

## 0.1.0-beta.1 — 2026-08-22

نخستین نسخهٔ بتا — یکپارچه‌سازی سامانه‌های پیامکی ایران با رابط ISmsClient:

 * هسته: تعریف قراردادهای اصلی (ISmsClient، ISmsBulkSender، ISmsOtpSender و ISmsDeliveryReporter)، پرچم‌های قابلیت (SmsCapabilities) و مدیریت استثناها (IranSmsException)

 * ارائه‌دهندگان: پشتیبانی کامل از کاوه‌نگار، قاصدک، SMS.ir، ملی‌پیامک و کلاینت Mock (همگی با پشتیبانی از Send، BulkSend، OtpSend و DeliveryStatus)

 * تزریق وابستگی (DI): ارائهٔ پکیج مستقل IranSms.DependencyInjection با قابلیت ثبت هوشمند بر اساس ویژگی‌ها و مدیریت مستقیم کلاینت توسط مصرف‌کننده از طریق AddIranSms

 * پروژه‌های نمونه: نمونهٔ Basic (کنسول بدون DI)، نمونهٔ AspNetCore (به‌صورت Minimal API) و نمونهٔ MultiProvider (مسیریابی پویا بر اساس قابلیت‌ها)

 * تست‌ها: دارای ۱۵۰ تست واحد همراه با پیاده‌سازی FakeTransport اختصاصی برای هر ارائه‌دهنده
