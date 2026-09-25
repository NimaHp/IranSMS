using FluentAssertions;
using IranSms.Providers.Ghasedak;
using IranSms.Providers.Kavenegar;
using IranSms.Providers.Melipayamak;
using IranSms.Providers.Mock;
using IranSms.Providers.SmsIr;
using IranSms.Tests.Ghasedak;
using IranSms.Tests.Kavenegar;
using IranSms.Tests.Melipayamak;
using IranSms.Tests.SmsIr;
using Xunit;

namespace IranSms.Tests;

/// <summary>
/// Every provider must validate and normalize identical inputs identically:
/// Persian/Arabic digits become ASCII, formatting characters are stripped,
/// blank messages are rejected, and whitespace-only sender lines are rejected.
/// </summary>
public class ProviderValidationTests
{
    private const string PersianRecipient = "۰۹۱۲۰۰۰۰۰۰۰";
    private const string ExpectedRecipient = "09120000000";
    private const string KavenegarApiKey = "test-api-key";

    private static KavenegarClient Kavenegar(FakeKavenegarTransport transport)
        => new KavenegarClient(transport, KavenegarApiKey);

    private static GhasedakClient Ghasedak(FakeGhasedakTransport transport)
        => new GhasedakClient(transport, KavenegarApiKey);

    private static SmsIrClient SmsIr(FakeSmsIrTransport transport)
        => new SmsIrClient(transport, KavenegarApiKey);

    private static MelipayamakClient Melipayamak(FakeMelipayamakTransport transport)
        => new MelipayamakClient(transport, "user", "pass");

    [Fact]
    public async Task SendAsync_NormalizesPersianDigits_ForEveryProvider()
    {
        var token = TestContext.Current.CancellationToken;

        var kavenegar = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"messageid\":1}]}",
        };
        await Kavenegar(kavenegar).SendAsync(PersianRecipient, "hi", "100001", token);
        kavenegar.LastParameters!["receptor"].Should().Be(ExpectedRecipient);

        var ghasedak = new FakeGhasedakTransport();
        await Ghasedak(ghasedak).SendAsync(PersianRecipient, "hi", "100001", token);
        ghasedak.LastJsonBody.Should().Contain($"\"{ExpectedRecipient}\"");

        var smsIr = new FakeSmsIrTransport
        {
            ResponseBody = "{\"status\":1,\"data\":{\"messageIds\":[1]}}",
        };
        await SmsIr(smsIr).SendAsync(PersianRecipient, "hi", "100001", token);
        smsIr.LastJson.Should().Contain($"\"{ExpectedRecipient}\"");

        var melipayamak = new FakeMelipayamakTransport { ResponseBody = "1" };
        await Melipayamak(melipayamak).SendAsync(PersianRecipient, "hi", "100001", token);
        melipayamak.LastForm!["to"].Should().Be(ExpectedRecipient);

        var mock = new MockSmsClient();
        await mock.SendAsync(PersianRecipient, "hi", "100001", token);
        mock.Messages.Should().ContainSingle().Which.Recipient.Should().Be(ExpectedRecipient);
    }

    [Fact]
    public async Task SendAsync_StripsFormattingCharacters_ForEveryProvider()
    {
        var token = TestContext.Current.CancellationToken;
        const string decorated = "  (۰۹۱۲) ۰۰۰۰۰۰۰  ";

        var kavenegar = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"messageid\":1}]}",
        };
        await Kavenegar(kavenegar).SendAsync(decorated, "hi", "100001", token);
        kavenegar.LastParameters!["receptor"].Should().Be(ExpectedRecipient);

        var mock = new MockSmsClient();
        await mock.SendAsync(decorated, "hi", "100001", token);
        mock.Messages.Should().ContainSingle().Which.Recipient.Should().Be(ExpectedRecipient);
    }

    [Fact]
    public async Task SendAsync_RejectsNullRecipient_ForEveryProvider()
    {
        var token = TestContext.Current.CancellationToken;

        await FluentActions.Awaiting(() => Kavenegar(new FakeKavenegarTransport()).SendAsync(null!, "hi", null, token))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => Ghasedak(new FakeGhasedakTransport()).SendAsync(null!, "hi", null, token))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => SmsIr(new FakeSmsIrTransport()).SendAsync(null!, "hi", "100001", token))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => Melipayamak(new FakeMelipayamakTransport()).SendAsync(null!, "hi", "100001", token))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => new MockSmsClient().SendAsync(null!, "hi", null, token))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_RejectsBlankMessage_ForEveryProvider()
    {
        var token = TestContext.Current.CancellationToken;

        await FluentActions.Awaiting(() => Kavenegar(new FakeKavenegarTransport()).SendAsync(ExpectedRecipient, "  ", null, token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("message");
        await FluentActions.Awaiting(() => Ghasedak(new FakeGhasedakTransport()).SendAsync(ExpectedRecipient, "  ", null, token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("message");
        await FluentActions.Awaiting(() => SmsIr(new FakeSmsIrTransport()).SendAsync(ExpectedRecipient, "  ", "100001", token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("message");
        await FluentActions.Awaiting(() => Melipayamak(new FakeMelipayamakTransport()).SendAsync(ExpectedRecipient, "  ", "100001", token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("message");
        await FluentActions.Awaiting(() => new MockSmsClient().SendAsync(ExpectedRecipient, "  ", null, token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("message");
    }

    [Fact]
    public async Task SendAsync_RejectsWhitespaceSenderLine_WhereSenderLineIsOptional()
    {
        var token = TestContext.Current.CancellationToken;

        await FluentActions.Awaiting(() => Kavenegar(new FakeKavenegarTransport()).SendAsync(ExpectedRecipient, "hi", "  ", token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("senderLine");
        await FluentActions.Awaiting(() => Ghasedak(new FakeGhasedakTransport()).SendAsync(ExpectedRecipient, "hi", "  ", token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("senderLine");
        await FluentActions.Awaiting(() => new MockSmsClient().SendAsync(ExpectedRecipient, "hi", "  ", token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("senderLine");
    }

    [Fact]
    public async Task SendAsync_TrimsSenderLine_WhereSenderLineIsRequired()
    {
        var token = TestContext.Current.CancellationToken;

        var smsIr = new FakeSmsIrTransport
        {
            ResponseBody = "{\"status\":1,\"data\":{\"messageIds\":[1]}}",
        };
        await SmsIr(smsIr).SendAsync(ExpectedRecipient, "hi", " 100001 ", token);
        smsIr.LastJson.Should().Contain("\"lineNumber\":100001");

        var melipayamak = new FakeMelipayamakTransport { ResponseBody = "1" };
        await Melipayamak(melipayamak).SendAsync(ExpectedRecipient, "hi", " 100001 ", token);
        melipayamak.LastForm!["from"].Should().Be("100001");
    }

    [Fact]
    public async Task SendBulkAsync_NormalizesEveryRecipient()
    {
        var token = TestContext.Current.CancellationToken;
        string[] recipients = { PersianRecipient, "۰۹۱۲۰۰۰۰۰۰۱" };

        var kavenegar = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"messageid\":1},{\"messageid\":2}]}",
        };
        await Kavenegar(kavenegar).SendBulkAsync(recipients, "hi", "100001", token);
        kavenegar.LastParameters!["receptor"].Should().Be("09120000000,09120000001");

        var melipayamak = new FakeMelipayamakTransport { ResponseBody = "1" };
        await Melipayamak(melipayamak).SendBulkAsync(recipients, "hi", "100001", token);
        melipayamak.LastForm!["to"].Should().Be("09120000000,09120000001");

        var mock = new MockSmsClient();
        await mock.SendBulkAsync(recipients, "hi", "100001", token);
        mock.Messages.Select(m => m.Recipient).Should().Equal("09120000000", "09120000001");
    }

    [Fact]
    public async Task SendBulkAsync_RejectsBlankRecipientEntry()
    {
        var token = TestContext.Current.CancellationToken;
        string[] recipients = { ExpectedRecipient, "   " };

        await FluentActions.Awaiting(() => Kavenegar(new FakeKavenegarTransport()).SendBulkAsync(recipients, "hi", null, token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("recipients");
        await FluentActions.Awaiting(() => Ghasedak(new FakeGhasedakTransport()).SendBulkAsync(recipients, "hi", null, token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("recipients");
        await FluentActions.Awaiting(() => SmsIr(new FakeSmsIrTransport()).SendBulkAsync(recipients, "hi", "100001", token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("recipients");
        await FluentActions.Awaiting(() => Melipayamak(new FakeMelipayamakTransport()).SendBulkAsync(recipients, "hi", "100001", token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("recipients");
        await FluentActions.Awaiting(() => new MockSmsClient().SendBulkAsync(recipients, "hi", null, token))
            .Should().ThrowAsync<ArgumentException>().WithParameterName("recipients");
    }

    [Fact]
    public async Task SendOtpAsync_NormalizesRecipient_AndRejectsBlankClientReference()
    {
        var token = TestContext.Current.CancellationToken;

        var kavenegar = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"messageid\":1}]}",
        };
        await Kavenegar(kavenegar).SendOtpAsync(PersianRecipient, new OtpRequest { TemplateId = "tpl", Code = "12345" }, token);
        kavenegar.LastParameters!["receptor"].Should().Be(ExpectedRecipient);

        var melipayamak = new FakeMelipayamakTransport { ResponseBody = "1" };
        await Melipayamak(melipayamak).SendOtpAsync(PersianRecipient, new OtpRequest { Code = "12345", SenderLine = "100001" }, token);
        melipayamak.LastForm!["to"].Should().Be(ExpectedRecipient);

        var mock = new MockSmsClient();
        var otpResult = await mock.SendOtpAsync(
            PersianRecipient,
            new OtpRequest { Code = "12345", ClientReferenceId = " order-42 " },
            token);
        mock.Messages.Should().ContainSingle().Which.Recipient.Should().Be(ExpectedRecipient);
        otpResult.ClientReferenceId.Should().Be("order-42");

        await FluentActions.Awaiting(() => new MockSmsClient().SendOtpAsync(
                ExpectedRecipient,
                new OtpRequest { Code = "1", ClientReferenceId = "  " },
                token))
            .Should().ThrowAsync<ArgumentException>();
    }
}
