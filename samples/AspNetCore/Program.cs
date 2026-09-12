using IranSms;
using IranSms.DependencyInjection;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Mock;

// ASP.NET Core sample: build a provider client yourself (consumer-owned), register it
// with AddIranSms(...) in the container, then resolve it as ISmsClient from a minimal
// API. Swapping provider is a one-line change (e.g. new KavenegarClient(...) with its
// API key).

var builder = WebApplication.CreateBuilder(args);

var kavenegarKey = builder.Configuration["Kavenegar:ApiKey"]
    ?? Environment.GetEnvironmentVariable("KAVENEGAR_API_KEY");

if (!string.IsNullOrWhiteSpace(kavenegarKey))
{
    // Production path: pooled HttpClient via IHttpClientFactory (handler pooling
    // with DNS refresh, per-call Timeout). The KavenegarClient instance stays
    // consumer-owned; the factory owns the handler lifetime.
    // Simple alternative: builder.Services.AddIranSms(new KavenegarClient(kavenegarKey)).
    builder.Services.AddHttpClient("kavenegar", client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddSingleton<KavenegarClient>(sp =>
        new KavenegarClient(kavenegarKey, sp.GetRequiredService<IHttpClientFactory>().CreateClient("kavenegar")));
    builder.Services.AddSingleton<ISmsClient>(sp => sp.GetRequiredService<KavenegarClient>());
    builder.Services.AddSingleton<ISmsBulkSender>(sp => sp.GetRequiredService<KavenegarClient>());
    builder.Services.AddSingleton<ISmsOtpSender>(sp => sp.GetRequiredService<KavenegarClient>());
    builder.Services.AddSingleton<ISmsDeliveryReporter>(sp => sp.GetRequiredService<KavenegarClient>());
    builder.Services.AddSingleton<ISmsAccountInfo>(sp => sp.GetRequiredService<KavenegarClient>());
}
else
{
    builder.Services.AddIranSms(new MockSmsClient("Mock"));
}

var app = builder.Build();

app.MapPost("/sms/send", async (
    SendSmsRequest request,
    ISmsClient sms,
    CancellationToken cancellationToken) =>
{
    var result = await sms.SendAsync(
        request.Recipient,
        request.Message,
        request.SenderLine,
        cancellationToken);
    return Results.Ok(new { result.MessageId, result.Cost });
});

app.MapPost("/sms/otp", async (
    SendOtpRequest request,
    ISmsClient sms,
    CancellationToken cancellationToken) =>
{
    if (sms is not ISmsOtpSender otpSender)
        return Results.Problem("The registered provider does not support OTP sends.");

    var result = await otpSender.SendOtpAsync(
        request.Recipient,
        new OtpRequest
        {
            Code = request.Code,
            TemplateId = request.TemplateId,
        },
        cancellationToken);
    return Results.Ok(new { result.MessageId, result.Cost });
});

app.MapGet("/sms/{messageId}/status", async (
    string messageId,
    ISmsClient sms,
    CancellationToken cancellationToken) =>
{
    if (sms is not ISmsDeliveryReporter reporter)
        return Results.Problem("The registered provider does not support delivery status.");

    var result = await reporter.GetMessageStatusAsync(
        new MessageIdentifier(messageId, MessageIdentifierType.ProviderMessageId),
        cancellationToken);
    return Results.Ok(new { result.State, result.RawStatus, result.Recipient, result.Price });
});

app.MapGet("/account/balance", async (ISmsClient sms, CancellationToken cancellationToken) =>
{
    if (sms is not ISmsAccountInfo account)
        return Results.Problem("The registered provider does not support account info.");

    var result = await account.GetBalanceAsync(cancellationToken);
    return Results.Ok(new { result.Credit, result.AccountType, result.ExpireDate });
});

app.MapGet("/account/lines", async (ISmsClient sms, CancellationToken cancellationToken) =>
{
    if (sms is not ISmsAccountInfo account)
        return Results.Problem("The registered provider does not support account info.");

    var lines = await account.GetSenderLinesAsync(cancellationToken);
    return Results.Ok(new { lines });
});

app.Run();

/// <summary>POST body for a single SMS send.</summary>
public sealed record SendSmsRequest(string Recipient, string Message, string? SenderLine = null);

/// <summary>POST body for an OTP send.</summary>
public sealed record SendOtpRequest(string Recipient, string Code, string? TemplateId = null);
