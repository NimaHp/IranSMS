# IranSMS

**یکپارچه‌سازی سامانه‌های پیامکی ایران در `.NET`** — نسخه `0.1.0-beta.1`

[![Build](https://github.com/NimaHp/IranSMS/actions/workflows/build.yml/badge.svg)](https://github.com/NimaHp/IranSMS/actions)
[![License](https://img.shields.io/github/license/NimaHp/IranSMS)](LICENSE)
[![NuGet version](https://img.shields.io/nuget/v/IranSms.Core)](https://www.nuget.org/packages/IranSms.Core)
[![NuGet downloads](https://img.shields.io/nuget/dt/IranSms.Core)](https://www.nuget.org/packages/IranSms.Core)
[![Release](https://img.shields.io/github/v/release/NimaHp/IranSMS)](https://github.com/NimaHp/IranSMS/releases)
[![Last commit](https://img.shields.io/github/last-commit/NimaHp/IranSMS)](https://github.com/NimaHp/IranSMS)
[![.NET](https://img.shields.io/badge/.NET-netstandard2.0-512BD4)](src/IranSms.Core)

**فارسی** | [English](README.en.md)

---

## درباره پروژه

کتابخانه‌ای سبک، مستقل از وابستگی‌های جانبی و بدون وابسته کردن پروژه به اپراتوری خاص برای ارسال پیامک در `.NET`. این کتابخانه یک قرارداد یکپارچه (<span dir="ltr">ISmsClient</span>)، قابلیت‌های قابل کشف (<span dir="ltr">SmsCapabilities</span>) و پشتیبانی از تزریق وابستگی (<span dir="ltr">DI</span>) را بدون وابستگی مستقیم به ارائه‌دهنده‌ای ارائه می‌دهد.

### سرویس‌دهنده‌های پشتیبانی‌شده

<table dir="rtl">
<thead>
<tr>
<th>پکیج</th>
<th>کلاینت</th>
<th>قابلیت‌ها</th>
<th>وابستگی افزوده</th>
<th>ملاحظات</th>
</tr>
</thead>
<tbody>
<tr>
<td><span dir="ltr">IranSms.Core</span></td>
<td><span dir="ltr">ISmsClient</span></td>
<td>قراردادهای پایه و اینترفیس قابلیت‌ها</td>
<td>—</td>
<td>بدون وابستگی</td>
</tr>
<tr>
<td><span dir="ltr">IranSms.Providers.Kavenegar</span></td>
<td><span dir="ltr">KavenegarClient</span></td>
<td>تکی · انبوه · الگویی (<span dir="ltr">OTP</span>) · وضعیت تحویل</td>
<td><span dir="ltr">System.Text.Json</span></td>
<td>ارسال انبوه تا ۲۰۰ گیرنده</td>
</tr>
<tr>
<td><span dir="ltr">IranSms.Providers.Ghasedak</span></td>
<td><span dir="ltr">GhasedakClient</span></td>
<td>تکی · انبوه · الگویی (<span dir="ltr">OTP</span>) · وضعیت تحویل</td>
<td><span dir="ltr">System.Text.Json</span></td>
<td>ارسال انبوه تا ۱۰۰ گیرنده؛ حداکثر ۱۰۰۰ کاراکتر</td>
</tr>
<tr>
<td><span dir="ltr">IranSms.Providers.SmsIr</span></td>
<td><span dir="ltr">SmsIrClient</span></td>
<td>تکی · انبوه · الگویی (<span dir="ltr">OTP</span>) · وضعیت تحویل</td>
<td><span dir="ltr">System.Text.Json</span></td>
<td>ارسال انبوه تا ۱۰۰ گیرنده؛ الزام عددی بودن <span dir="ltr">senderLine</span></td>
</tr>
<tr>
<td><span dir="ltr">IranSms.Providers.Melipayamak</span></td>
<td><span dir="ltr">MelipayamakClient</span></td>
<td>تکی · انبوه · الگویی (<span dir="ltr">OTP</span>) · وضعیت تحویل</td>
<td>—</td>
<td>الزامی بودن <span dir="ltr">senderLine</span></td>
</tr>
<tr>
<td><span dir="ltr">IranSms.Providers.Mock</span></td>
<td><span dir="ltr">MockSmsClient</span></td>
<td>تکی · انبوه · الگویی (<span dir="ltr">OTP</span>) · وضعیت تحویل</td>
<td>—</td>
<td>مبتنی بر حافظه با شناسه معین <span dir="ltr">mock-{n}</span></td>
</tr>
<tr>
<td><span dir="ltr">IranSms.DependencyInjection</span></td>
<td><span dir="ltr">AddIranSms</span></td>
<td>ثبت هوشمند مبتنی بر قابلیت</td>
<td><span dir="ltr">DI.Abstractions</span></td>
<td>وابستگی انحصاری به <span dir="ltr">Core</span></td>
</tr>
</tbody>
</table>

### معماری

```
              IranSms.Core  (netstandard2.0 — بدون هیچ وابستگی)
                 ↑         ↑
                 │         │
      IranSms.DependencyInjection   IranSms.Providers.*  (هرکدام فقط وابسته به Core)
                 ↑         │
                 └──── برنامه شما ──── new KavenegarClient(apiKey)
```

* هر ارائه‌دهنده صرفاً به <span dir="ltr">Core</span> وابسته است؛ نصب پکیج <span dir="ltr">Kavenegar</span> هیچ وابستگی اضافه مانند <span dir="ltr">Microsoft.Extensions.*</span> را به پروژه تحمیل نمی‌کند.
* پکیج <span dir="ltr">IranSms.DependencyInjection</span> تنها به <span dir="ltr">Core</span> و <span dir="ltr">Microsoft.Extensions.DependencyInjection.Abstractions</span> وابسته است.
* مدیریت طول عمر <span dir="ltr">HttpClient</span> و اعتبارنامه‌ها بر عهده برنامه شماست و ثبت آن از طریق <span dir="ltr">AddIranSms</span> انجام می‌شود.

### قابلیت‌ها

<table dir="rtl">
<thead>
<tr>
<th>قابلیت</th>
<th>پرچم (<span dir="ltr">Flag</span>)</th>
<th>اینترفیس</th>
<th>وضعیت</th>
</tr>
</thead>
<tbody>
<tr><td>ارسال تکی</td><td><span dir="ltr">Send</span></td><td><span dir="ltr">ISmsClient.SendAsync</span></td><td>✅ پیاده‌سازی‌شده</td></tr>
<tr><td>ارسال انبوه</td><td><span dir="ltr">BulkSend</span></td><td><span dir="ltr">ISmsBulkSender.SendBulkAsync</span></td><td>✅ پیاده‌سازی‌شده</td></tr>
<tr><td>ارسال الگویی / کد تأیید</td><td><span dir="ltr">OtpSend</span></td><td><span dir="ltr">ISmsOtpSender.SendOtpAsync</span></td><td>✅ پیاده‌سازی‌شده</td></tr>
<tr><td>وضعیت تحویل</td><td><span dir="ltr">DeliveryStatus</span></td><td><span dir="ltr">ISmsDeliveryReporter.GetMessageStatusAsync</span></td><td>✅ پیاده‌سازی‌شده</td></tr>
<tr><td>ارسال ناهمگون</td><td><span dir="ltr">HeterogeneousSend</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>ارسال زمان‌بندی‌شده</td><td><span dir="ltr">ScheduledSend</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>تاریخچه پیام‌ها</td><td><span dir="ltr">MessageHistory</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>دریافت پیام</td><td><span dir="ltr">Receive</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>اطلاعات حساب</td><td><span dir="ltr">AccountInfo</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>مدیریت خطوط</td><td><span dir="ltr">LineManagement</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>مدیریت قالب‌ها</td><td><span dir="ltr">TemplateManagement</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>پیام فلش</td><td><span dir="ltr">FlashMessage</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>پیام صوتی</td><td><span dir="ltr">VoiceMessage</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
<tr><td>اعتبارسنجی پارامترهای قالب</td><td><span dir="ltr">OtpTemplateInspection</span></td><td>—</td><td>🗓 نقشه راه</td></tr>
</tbody>
</table>

بررسی قابلیت‌ها بدون استفاده از <span dir="ltr">HasFlag</span> (جهت جلوگیری از سربار <span dir="ltr">Boxed-Type</span> در <span dir="ltr">netstandard2.0</span>):

```csharp
if ((client.Capabilities & SmsCapabilities.OtpSend) == SmsCapabilities.OtpSend) { ... }
// یا
if (client.Supports(SmsCapabilities.OtpSend)) { ... }
// یا
if (client is ISmsOtpSender otp) { ... }
```

### ویژگی‌های کلیدی

* ✅ **بدون وابستگی اضافی:** هسته پروژه بدون هیچ وابستگی خارجی است؛ هر پکیج ارائه‌دهنده تنها به <span dir="ltr">Core</span> وابستگی دارد.
* ✅ **مدیریت مستقیم کلاینت:** ساخت و نگهداری چرخه عمر <span dir="ltr">HttpClient</span> و کلیدهای دسترسی در اختیار مصرف‌کننده است.
* ✅ **ثبت هوشمند مبتنی بر قابلیت:** در تزریق وابستگی، فقط اینترفیس‌هایی ثبت می‌شوند که توسط کلاینت مربوطه پیاده‌سازی شده باشند.
* ✅ **قابلیت تست‌پذیری بالا:** ارائه کلاینت <span dir="ltr">Mock</span> با شناسه قطعی <span dir="ltr">mock-{n}</span> جهت تست‌های محلی.
* ✅ **تایماوت HTTP:** چرخه عمر <span dir="ltr">HttpClient</span> در اختیار مصرفکننده است؛ اگر <span dir="ltr">HttpClient</span> تزریق نشود، تایماوت پیشفرض (۱۰۰ ثانیه) اعمال میشود — برای تنظیم دقیق، <span dir="ltr">HttpClient</span> پیکربندیشده خودتان را به سازنده کلاینت بدهید.
* ✅ **پشتیبانی از <span dir="ltr">netstandard2.0</span>:** قابل استفاده در تمام نسخه‌های <span dir="ltr">.NET</span> (از <span dir="ltr">.NET Framework</span> تا <span dir="ltr">.NET 10</span>).
* ✅ **مدیریت خطای دقیق:** عدم ارائه مقادیر ساختگی در صورت نبود <span dir="ltr">MessageId</span> و صدور صریح استثنا.

## نصب

```bash
dotnet add package IranSms.Core
dotnet add package IranSms.Providers.Kavenegar   # یا Ghasedak / SmsIr / Melipayamak
dotnet add package IranSms.Providers.Mock        # جهت تست‌های محلی
dotnet add package IranSms.DependencyInjection   # اختیاری — صرفاً در صورت نیاز به DI
```

## راهنمای سریع

### استفاده مستقیم (بدون DI)

```csharp
using IranSms.Providers.Kavenegar;

var client = new KavenegarClient(Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY")!);

var result = await client.SendAsync("09121234567", "سلام از IranSMS!");
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

### استفاده در ASP.NET Core با تزریق وابستگی (DI)

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

> **نکته:** هرگز کلیدهای دسترسی (<span dir="ltr">API Keys</span>) را در سورس‌کد به صورت کُدسخت (<span dir="ltr">Hard-code</span>) قرار ندهید. آن‌ها را از <span dir="ltr">IConfiguration</span>، <span dir="ltr">UserSecrets</span> یا <span dir="ltr">Key Vault</span> بخوانید.

### استفاده هم‌زمان از چند سرویس‌دهنده

```csharp
services.AddIranSms(new MockSmsClient("Mock"));
services.AddIranSms(new KavenegarClient(kavenegarKey));
services.AddIranSms(new GhasedakClient(ghasedakKey));

var clients = provider.GetServices<ISmsClient>();
var otpSender = clients.FirstOrDefault(c => c.Supports(SmsCapabilities.OtpSend)) as ISmsOtpSender;
```

## مدیریت استثناها

```csharp
try
{
    var r = await client.SendAsync(recipient, message);
}
catch (IranSmsException ex) when (ex.ProviderName == "Ghasedak")
{
    // فیلدهای ex.ProviderStatusCode و ex.RawResponseBody مقداردهی شده‌اند
    // از ثبت RawResponseBody در لاگ‌های عمومی خودداری کنید؛ زیرا ممکن است حاوی متن پیام یا شماره باشد
    logger.LogWarning("خطای قاصدک {Code}: {Message}", ex.ProviderStatusCode, ex.Message);
}
catch (HttpRequestException)
{
    // خطای شبکه یا تایم‌اوت — فاقد RawResponseBody
}
```

## پروژه‌های نمونه

<table dir="rtl">
<thead>
<tr>
<th>نمونه</th>
<th>توضیحات</th>
<th>نحوه اجرا</th>
</tr>
</thead>
<tbody>
<tr><td><span dir="ltr">samples/Basic</span></td><td>برنامه کنسول بدون <span dir="ltr">DI</span> جهت نمایش تمامی قابلیت‌ها با <span dir="ltr">Mock</span></td><td><span dir="ltr">dotnet run --project samples/Basic</span></td></tr>
<tr><td><span dir="ltr">samples/AspNetCore</span></td><td>پروژه <span dir="ltr">Minimal API</span> به همراه <span dir="ltr">AddIranSms</span></td><td><span dir="ltr">dotnet run --project samples/AspNetCore</span></td></tr>
<tr><td><span dir="ltr">samples/MultiProvider</span></td><td>ثبت ۵ ارائه‌دهنده و مسیریابی پویا بر اساس قابلیت‌ها</td><td><span dir="ltr">dotnet run --project samples/MultiProvider</span></td></tr>
</tbody>
</table>

مقداردهی کلیدها از طریق متغیرهای محیطی:

```bash
export KAVENEGAR_API_KEY=...
export GHASEDAK_API_KEY=...
export SMSIR_API_KEY=...
export MELIPAYAMAK_USERNAME=... MELIPAYAMAK_PASSWORD=...
```

## وضعیت پکیج‌ها

<table dir="rtl">
<thead>
<tr><th>پکیج</th><th>وضعیت</th><th>کاربرد</th></tr>
</thead>
<tbody>
<tr><td><span dir="ltr">IranSms.Core</span></td><td>✅</td><td>قراردادها و قابلیت‌های اصلی — بدون وابستگی</td></tr>
<tr><td><span dir="ltr">IranSms.Providers.Kavenegar</span></td><td>✅</td><td>ارسال پیامک از طریق کاوه‌نگار</td></tr>
<tr><td><span dir="ltr">IranSms.Providers.Ghasedak</span></td><td>✅</td><td>ارسال پیامک از طریق قاصدک</td></tr>
<tr><td><span dir="ltr">IranSms.Providers.SmsIr</span></td><td>✅</td><td>ارسال پیامک از طریق <span dir="ltr">SMS.ir</span></td></tr>
<tr><td><span dir="ltr">IranSms.Providers.Melipayamak</span></td><td>✅</td><td>ارسال پیامک از طریق ملی‌پیامک</td></tr>
<tr><td><span dir="ltr">IranSms.Providers.Mock</span></td><td>✅</td><td>پیاده‌سازی مبتنی بر حافظه جهت تست</td></tr>
<tr><td><span dir="ltr">IranSms.DependencyInjection</span></td><td>✅</td><td>ثبت هوشمند در سیستم تزریق وابستگی (<span dir="ltr">DI</span>)</td></tr>
</tbody>
</table>

## تست و پوشش کد

ویندوز (PowerShell):

```powershell
dotnet build -c Release
dotnet run --project tests\IranSms.Tests -c Release --no-build --framework net10.0
dotnet run --project tests\IranSms.Tests -c Release --no-build --framework net8.0
```

لینوکس / مک (bash):

```bash
dotnet build -c Release
dotnet run --project tests/IranSms.Tests -c Release --no-build --framework net10.0
dotnet run --project tests/IranSms.Tests -c Release --no-build --framework net8.0
```

شامل ۱۵۰ تست پاس‌شده (<span dir="ltr">xunit v3</span> + <span dir="ltr">FluentAssertions</span>).

## فرآیند CI/CD

* **گردش کار <span dir="ltr">build.yml</span>:** اجرا و اعتبارسنجی تست‌ها در هر <span dir="ltr">push</span> و <span dir="ltr">pull_request</span>
* **گردش کار <span dir="ltr">release.yml</span>:** انتشار خودکار پکیج‌ها در <span dir="ltr">NuGet.org</span> و ایجاد <span dir="ltr">GitHub Release</span> هنگام ثبت تگ‌های <span dir="ltr">v*</span>

## مجوز (License)

این پروژه تحت مجوز <span dir="ltr">MIT</span> منتشر شده است.
