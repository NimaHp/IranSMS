using System.Net;
using System.Text;
using FluentAssertions;
using IranSms.Providers.Ghasedak;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Melipayamak;
using IranSms.Providers.SmsIr;
using Xunit;

namespace IranSms.Tests;

public class ProviderTransportTests
{
    [Fact]
    public async Task KavenegarTransport_UsesFormAndDoesNotExposeErrorBody()
    {
        var handler = new RecordingHandler("secret-provider-response");
        using var client = new KavenegarClient("api-key", new HttpClient(handler));

        var act = async () => await client.SendAsync("09120000000", "hello", null, TestContext.Current.CancellationToken);
        var exception = (await act.Should().ThrowAsync<IranSmsException>()).Which;

        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().Contain("/api-key/sms/send.json");
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
        exception.Message.Should().NotContain("secret-provider-response");
    }

    [Fact]
    public async Task GhasedakTransport_UsesApiKeyAndDoesNotExposeErrorBody()
    {
        var handler = new RecordingHandler("secret-provider-response");
        using var httpClient = new HttpClient(handler);
        using var client = new GhasedakClient("api-key", httpClient);

        var act = async () => await client.SendAsync("09120000000", "hello", null, TestContext.Current.CancellationToken);
        var exception = (await act.Should().ThrowAsync<IranSmsException>()).Which;

        handler.LastRequest!.Headers.GetValues("ApiKey").Should().ContainSingle("api-key");
        handler.LastRequest.RequestUri!.AbsoluteUri.Should().EndWith("SendSingleSMS");
        exception.Message.Should().NotContain("secret-provider-response");
    }

    [Fact]
    public async Task SmsIrTransport_UsesApiKeyAndDoesNotExposeErrorBody()
    {
        var handler = new RecordingHandler("secret-provider-response");
        using var httpClient = new HttpClient(handler);
        using var client = new SmsIrClient("api-key", httpClient);

        var act = async () => await client.SendAsync("09120000000", "hello", "5000", TestContext.Current.CancellationToken);
        var exception = (await act.Should().ThrowAsync<IranSmsException>()).Which;

        handler.LastRequest!.Headers.GetValues("X-API-KEY").Should().ContainSingle("api-key");
        handler.LastRequest.RequestUri!.AbsoluteUri.Should().EndWith("/v1/send/bulk");
        exception.Message.Should().NotContain("secret-provider-response");
    }

    [Fact]
    public async Task MelipayamakTransport_UsesFormAndDoesNotExposeErrorBody()
    {
        var handler = new RecordingHandler("secret-provider-response");
        using var httpClient = new HttpClient(handler);
        using var client = new MelipayamakClient("user", "password", httpClient);

        var act = async () => await client.SendAsync("09120000000", "hello", "5000", TestContext.Current.CancellationToken);
        var exception = (await act.Should().ThrowAsync<IranSmsException>()).Which;

        handler.LastRequest!.RequestUri!.AbsoluteUri.Should().EndWith("/api/SendSMS/SendSMS");
        handler.LastRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/x-www-form-urlencoded");
        exception.Message.Should().NotContain("secret-provider-response");
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string _body;

        public RecordingHandler(string body)
        {
            _body = body;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(_body, Encoding.UTF8, "text/plain"),
            });
        }
    }
}
