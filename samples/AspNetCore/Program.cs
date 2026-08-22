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
    builder.Services.AddIranSms(new KavenegarClient(kavenegarKey));
else
    builder.Services.AddIranSms(new MockSmsClient("Mock"));

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

app.Run();

/// <summary>POST body for a single SMS send.</summary>
public sealed record SendSmsRequest(string Recipient, string Message, string? SenderLine = null);

/// <summary>POST body for an OTP send.</summary>
public sealed record SendOtpRequest(string Recipient, string Code, string? TemplateId = null);
