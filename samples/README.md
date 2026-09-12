﻿# نمونه‌های IranSMS

**فارسی** | [English](README.en.md)

## فهرست

<table dir="rtl">
<thead>
<tr><th>نمونه</th><th>توضیح</th><th>اجرا</th></tr>
</thead>
<tbody>
<tr><td><span dir="ltr">Basic</span></td><td>کنسول بدون <span dir="ltr">DI</span> — همه قابلیت‌ها با <span dir="ltr">Mock</span></td><td><span dir="ltr">dotnet run --project samples/Basic</span></td></tr>
<tr><td><span dir="ltr">AspNetCore</span></td><td><span dir="ltr">Minimal API</span> با <span dir="ltr">AddIranSms</span> + نمونه <span dir="ltr">IHttpClientFactory</span></td><td><span dir="ltr">dotnet run --project samples/AspNetCore</span></td></tr>
<tr><td><span dir="ltr">MultiProvider</span></td><td>ثبت ۵ ارائه‌دهنده و مسیریابی بر اساس قابلیت</td><td><span dir="ltr">dotnet run --project samples/MultiProvider</span></td></tr>
</tbody>
</table>

## کلیدهای دسترسی

کلید را هرگز در کد قرار ندهید. هر سه نمونه در صورت نبود متغیر محیطی به `Mock` برمی‌گردند:

```bash
export KAVENEGAR_API_KEY=...
export GHASEDAK_API_KEY=...
export SMSIR_API_KEY=...
export MELIPAYAMAK_USERNAME=... MELIPAYAMAK_PASSWORD=...
```

برای `AspNetCore` می‌توانید از `UserSecrets` یا `appsettings.json` هم استفاده کنید:

```bash
dotnet user-secrets --project samples/AspNetCore set "Kavenegar:ApiKey" "YOUR_KEY"
# یا در appsettings.Development.json:
# { "Kavenegar": { "ApiKey": "YOUR_KEY" } }
```

## مدیریت HttpClient

نمونه `AspNetCore` برای ارائه‌دهنده واقعی از `IHttpClientFactory` استفاده می‌کند تا pooling هندلر و رفرش DNS خودکار باشد:

```csharp
builder.Services.AddHttpClient("kavenegar", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<KavenegarClient>(sp =>
    new KavenegarClient(kavenegarKey, sp.GetRequiredService<IHttpClientFactory>().CreateClient("kavenegar")));
builder.Services.AddSingleton<ISmsClient>(sp => sp.GetRequiredService<KavenegarClient>());
// + ثبت ISmsBulkSender / ISmsOtpSender / ISmsDeliveryReporter / ISmsAccountInfo به همین شکل
```

مسیر ساده (بدون factory):

```csharp
builder.Services.AddIranSms(new KavenegarClient(kavenegarKey));
```

## هشدار امنیتی

خطای `IranSmsException.RawResponseBody` ممکن است متن پیام یا شماره را در بر داشته باشد — آن را در لاگ عمومی یا پاسخ HTTP ننویسید.

## لایسنس

MIT — فایل [LICENSE](../LICENSE) را ببینید.
