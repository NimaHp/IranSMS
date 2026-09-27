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
/// <c>OtpSend</c> alone cannot tell a caller which request shape to build, so the two
/// shapes are announced by their own capability flags. The matrix below is the contract.
/// </summary>
public class OtpShapeCapabilityTests
{
    private static KavenegarClient Kavenegar() => new KavenegarClient(new FakeKavenegarTransport(), "test-api-key");

    private static GhasedakClient Ghasedak() => new GhasedakClient(new FakeGhasedakTransport(), "test-api-key");

    private static SmsIrClient SmsIr() => new SmsIrClient(new FakeSmsIrTransport(), "test-api-key");

    private static MelipayamakClient Melipayamak() => new MelipayamakClient(new FakeMelipayamakTransport(), "user", "pass");

    private static MockSmsClient Mock() => new MockSmsClient();

    [Theory]
    [InlineData("Kavenegar", true, false)]
    [InlineData("Ghasedak", true, false)]
    [InlineData("SmsIr", true, false)]
    [InlineData("Melipayamak", false, true)]
    [InlineData("Mock", true, true)]
    public void OtpShapeFlags_MatchTheDocumentedMatrix(string provider, bool template, bool code)
    {
        ISmsClient client = provider switch
        {
            "Kavenegar" => Kavenegar(),
            "Ghasedak" => Ghasedak(),
            "SmsIr" => SmsIr(),
            "Melipayamak" => Melipayamak(),
            _ => Mock(),
        };

        client.Supports(SmsCapabilities.OtpSend).Should().BeTrue();
        client.Supports(SmsCapabilities.OtpTemplateSend).Should().Be(template);
        client.Supports(SmsCapabilities.OtpCodeSend).Should().Be(code);
    }

    [Fact]
    public void EveryOtpCapableClient_ImplementsTheSenderInterface()
    {
        foreach (ISmsClient client in new ISmsClient[] { Kavenegar(), Ghasedak(), SmsIr(), Melipayamak(), Mock() })
            client.Should().BeAssignableTo<ISmsOtpSender>();
    }

    [Fact]
    public void TemplateProviders_RejectCodeRequests()
    {
        var request = new OtpCodeRequest("12345");

        FluentActions.Invoking(() => Kavenegar().SendOtpAsync("09120000000", request)).Should().ThrowAsync<ArgumentException>();
        FluentActions.Invoking(() => Ghasedak().SendOtpAsync("09120000000", request)).Should().ThrowAsync<ArgumentException>();
        FluentActions.Invoking(() => SmsIr().SendOtpAsync("09120000000", request)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Melipayamak_RejectsTemplateRequests()
    {
        var request = new OtpTemplateRequest("verify").SetParameter("token", "12345");

        FluentActions.Invoking(() => Melipayamak().SendOtpAsync("09120000000", request)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Mock_AcceptsBothShapes()
    {
        var client = new MockSmsClient();

        var code = await client.SendOtpAsync("09120000000", new OtpCodeRequest("12345"), TestContext.Current.CancellationToken);
        var template = await client.SendOtpAsync("09120000000", new OtpTemplateRequest("verify").SetParameter("token", "12345"), TestContext.Current.CancellationToken);

        client.Messages.Should().HaveCount(2);
        client.Messages[0].MessageText.Should().Be("12345");
        client.Messages[1].MessageText.Should().Be("12345");
        client.Messages[1].TemplateId.Should().Be("verify");
        code.MessageId.Should().Be("mock-1");
        template.MessageId.Should().Be("mock-2");
    }
}
