# IranSMS Samples

**English** | [فارسی](README.md)

## Index

| Sample | Description | Run |
|---|---|---|
| `Basic` | Console without DI — all capabilities via `Mock` | `dotnet run --project samples/Basic` |
| `AspNetCore` | Minimal API with `AddIranSms` + `IHttpClientFactory` sample | `dotnet run --project samples/AspNetCore` |
| `MultiProvider` | 5 providers with capability-aware routing | `dotnet run --project samples/MultiProvider` |

## API Keys

Never hard-code keys. All three samples fall back to `Mock` when env vars are absent:

```bash
export KAVENEGAR_API_KEY=...
export GHASEDAK_API_KEY=...
export SMSIR_API_KEY=...
export MELIPAYAMAK_USERNAME=... MELIPAYAMAK_PASSWORD=...
```

For `AspNetCore` you can also use `UserSecrets` or `appsettings.json`:

```bash
dotnet user-secrets --project samples/AspNetCore set "Kavenegar:ApiKey" "YOUR_KEY"
# or in appsettings.Development.json:
# { "Kavenegar": { "ApiKey": "YOUR_KEY" } }
```

## HttpClient management

The `AspNetCore` sample uses `IHttpClientFactory` for the real provider so handler pooling and DNS refresh are automatic:

```csharp
builder.Services.AddHttpClient("kavenegar", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<KavenegarClient>(sp =>
    new KavenegarClient(kavenegarKey, sp.GetRequiredService<IHttpClientFactory>().CreateClient("kavenegar")));
builder.Services.AddSingleton<ISmsClient>(sp => sp.GetRequiredService<KavenegarClient>());
// + ISmsBulkSender / ISmsOtpSender / ISmsDeliveryReporter / ISmsAccountInfo the same way
```

Simple path (no factory):

```csharp
builder.Services.AddIranSms(new KavenegarClient(kavenegarKey));
```

## Security Notice

`IranSmsException.RawResponseBody` may contain message text or phone numbers — do not log it to public sinks or return it in HTTP responses.

## License

MIT — see [LICENSE](../LICENSE).
