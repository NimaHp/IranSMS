using IranSms;
using IranSms.Providers.Mock;

// ASP.NET Core sample: register a provider through Add<Provider>(), resolve it
// as ISmsClient from the container, and call it from a minimal API. Swapping
// provider is a one-line change (e.g. AddKavenegar(...) + its credentials).

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMock(options =>
{
    options.ProviderName = "SmsApi";
    options.Timeout = TimeSpan.FromSeconds(10);
});

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
