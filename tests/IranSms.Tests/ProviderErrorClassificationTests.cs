using FluentAssertions;
using IranSms.Providers.Ghasedak;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Melipayamak;
using IranSms.Providers.SmsIr;
using IranSms.Tests.Ghasedak;
using IranSms.Tests.Kavenegar;
using IranSms.Tests.Melipayamak;
using IranSms.Tests.SmsIr;
using Xunit;

namespace IranSms.Tests;

/// <summary>
/// Every provider must classify its failures with a normalized
/// <see cref="SmsErrorKind"/> and name the operation that failed, so callers can
/// branch on one vocabulary instead of parsing provider-specific messages.
/// </summary>
public class ProviderErrorClassificationTests
{
    private static KavenegarClient Kavenegar(FakeKavenegarTransport transport)
        => new KavenegarClient(transport, "test-api-key");

    private static GhasedakClient Ghasedak(FakeGhasedakTransport transport)
        => new GhasedakClient(transport, "test-api-key");

    private static SmsIrClient SmsIr(FakeSmsIrTransport transport)
        => new SmsIrClient(transport, "test-api-key");

    private static MelipayamakClient Melipayamak(FakeMelipayamakTransport transport)
        => new MelipayamakClient(transport, "user", "pass");

    private static async Task<IranSmsException> CaptureAsync(Func<Task> act)
        => (await FluentActions.Awaiting(act).Should().ThrowAsync<IranSmsException>()).Which;

    [Theory]
    [InlineData(401, SmsErrorKind.Unauthorized)]
    [InlineData(403, SmsErrorKind.Unauthorized)]
    [InlineData(402, SmsErrorKind.InsufficientBalance)]
    [InlineData(408, SmsErrorKind.Timeout)]
    [InlineData(429, SmsErrorKind.RateLimited)]
    [InlineData(504, SmsErrorKind.Timeout)]
    [InlineData(500, SmsErrorKind.ProviderUnavailable)]
    [InlineData(503, SmsErrorKind.ProviderUnavailable)]
    [InlineData(400, SmsErrorKind.ProviderRejected)]
    [InlineData(404, SmsErrorKind.ProviderRejected)]
    [InlineData(424, SmsErrorKind.ProviderRejected)]
    public void FromHttpStatus_MapsTheDocumentedMatrix(int status, SmsErrorKind expected)
        => SmsErrorKindExtensions.FromHttpStatus(status).Should().Be(expected);

    [Fact]
    public async Task Kavenegar_ApiError_IsClassifiedAndNamed()
    {
        var transport = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":402,\"message\":\"not enough credit\"},\"entries\":[]}",
        };

        var error = await CaptureAsync(() => Kavenegar(transport).SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken));

        error.ProviderName.Should().Be("Kavenegar");
        error.ProviderStatusCode.Should().Be(402);
        error.Kind.Should().Be(SmsErrorKind.InsufficientBalance);
        error.Operation.Should().Be("sms/send");
        error.IsTransient.Should().BeFalse();
    }

    [Fact]
    public async Task Kavenegar_AuthError_IsUnauthorized()
    {
        var transport = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":401,\"message\":\"auth failed\"},\"entries\":[]}",
        };

        var error = await CaptureAsync(() => Kavenegar(transport).SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Kavenegar_MissingMessageId_IsMalformedResponse()
    {
        var transport = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"status\":200}]}",
        };

        var error = await CaptureAsync(() => Kavenegar(transport).SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.MalformedResponse);
        error.Operation.Should().Be("Send");
    }

    [Fact]
    public async Task Kavenegar_MalformedJson_IsMalformedResponse()
    {
        var transport = new FakeKavenegarTransport { ResponseBody = "not-json" };

        var error = await CaptureAsync(() => Kavenegar(transport).SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.MalformedResponse);
        error.Operation.Should().Be("sms/send");
        error.InnerException.Should().NotBeNull();
    }

    [Fact]
    public async Task Kavenegar_BalanceFailure_NamesTheAccountOperation()
    {
        var transport = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"remaincredit\":\"nope\"}]}",
        };

        var error = await CaptureAsync(() => Kavenegar(transport).GetBalanceAsync(TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.MalformedResponse);
        error.Operation.Should().Be("account/info");
    }

    [Fact]
    public async Task Ghasedak_ApiError_IsClassifiedAndNamed()
    {
        var transport = new FakeGhasedakTransport
        {
            PostResponse = "{\"IsSuccess\":false,\"StatusCode\":401,\"Data\":null}",
        };

        var error = await CaptureAsync(() => Ghasedak(transport).SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken));

        error.ProviderName.Should().Be("Ghasedak");
        error.ProviderStatusCode.Should().Be(401);
        error.Kind.Should().Be(SmsErrorKind.Unauthorized);
        error.Operation.Should().Be("SendSingleSMS");
    }

    [Fact]
    public async Task Ghasedak_ServerError_IsTransient()
    {
        var transport = new FakeGhasedakTransport
        {
            PostResponse = "{\"IsSuccess\":false,\"StatusCode\":503,\"Data\":null}",
        };

        var error = await CaptureAsync(() => Ghasedak(transport).SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.ProviderUnavailable);
        error.IsTransient.Should().BeTrue();
    }

    [Fact]
    public async Task Ghasedak_UnrecognizedResponse_IsMalformedResponse()
    {
        var transport = new FakeGhasedakTransport { PostResponse = "{}" };

        var error = await CaptureAsync(() => Ghasedak(transport).SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.MalformedResponse);
        error.Operation.Should().Be("SendSingleSMS");
    }

    [Fact]
    public async Task SmsIr_ApiError_IsClassifiedAndNamed()
    {
        var transport = new FakeSmsIrTransport
        {
            ResponseBody = "{\"status\":2,\"message\":\"bad request\"}",
        };

        var error = await CaptureAsync(() => SmsIr(transport).SendAsync("09120000000", "hi", "100001", TestContext.Current.CancellationToken));

        error.ProviderName.Should().Be("SmsIr");
        error.ProviderStatusCode.Should().Be(2);
        error.Kind.Should().Be(SmsErrorKind.ProviderRejected);
        error.Operation.Should().Be("send/bulk");
    }

    [Fact]
    public async Task SmsIr_UnparseableEnvelope_IsMalformedResponse()
    {
        var transport = new FakeSmsIrTransport { ResponseBody = "not-json" };

        var error = await CaptureAsync(() => SmsIr(transport).SendAsync("09120000000", "hi", "100001", TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.MalformedResponse);
        error.Operation.Should().Be("send/bulk");
    }

    [Fact]
    public async Task SmsIr_MissingMessageId_IsMalformedResponse()
    {
        var transport = new FakeSmsIrTransport
        {
            ResponseBody = "{\"status\":1,\"data\":{\"messageIds\":[0]}}",
        };

        var error = await CaptureAsync(() => SmsIr(transport).SendAsync("09120000000", "hi", "100001", TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.MalformedResponse);
        error.Operation.Should().Be("send/bulk");
    }

    [Fact]
    public async Task Melipayamak_ApiError_IsProviderRejected()
    {
        var transport = new FakeMelipayamakTransport { ResponseBody = "17" };

        var error = await CaptureAsync(() => Melipayamak(transport).SendAsync("09120000000", "hi", "100001", TestContext.Current.CancellationToken));

        error.ProviderName.Should().Be("Melipayamak");
        error.ProviderStatusCode.Should().Be(17);
        error.Kind.Should().Be(SmsErrorKind.ProviderRejected);
        error.IsTransient.Should().BeFalse();
    }

    [Fact]
    public async Task Melipayamak_UnrecognizedResponse_IsMalformedResponse()
    {
        var transport = new FakeMelipayamakTransport { ResponseBody = "not-a-number" };

        var error = await CaptureAsync(() => Melipayamak(transport).SendAsync("09120000000", "hi", "100001", TestContext.Current.CancellationToken));

        error.Kind.Should().Be(SmsErrorKind.MalformedResponse);
    }
}
