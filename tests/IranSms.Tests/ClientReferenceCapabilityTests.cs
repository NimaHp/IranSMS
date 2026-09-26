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
/// Client-reference support is opt-in per provider and verified against each
/// provider's official web-service documentation: Kavenegar accepts a numeric
/// <c>localid</c> and can look status up by it, Ghasedak accepts an opaque
/// <c>clientReferenceId</c> but cannot look status up by it, and SMS.ir /
/// Melipayamak have no such field at all.
/// </summary>
public class ClientReferenceCapabilityTests
{
    private static KavenegarClient Kavenegar(FakeKavenegarTransport transport)
        => new KavenegarClient(transport, "test-api-key");

    private static GhasedakClient Ghasedak(FakeGhasedakTransport transport)
        => new GhasedakClient(transport, "test-api-key");

    private static SmsIrClient SmsIr(FakeSmsIrTransport transport)
        => new SmsIrClient(transport, "test-api-key");

    private static MelipayamakClient Melipayamak(FakeMelipayamakTransport transport)
        => new MelipayamakClient(transport, "user", "pass");

    [Fact]
    public void Kavenegar_SupportsSendAndLookup()
    {
        var client = Kavenegar(new FakeKavenegarTransport());

        client.Supports(SmsCapabilities.ClientReference).Should().BeTrue();
        client.Supports(SmsCapabilities.ClientReferenceLookup).Should().BeTrue();
        client.Should().BeAssignableTo<ISmsClientReferenceSender>();
    }

    [Fact]
    public void Ghasedak_SupportsSendButNotLookup()
    {
        var client = Ghasedak(new FakeGhasedakTransport());

        client.Supports(SmsCapabilities.ClientReference).Should().BeTrue();
        client.Supports(SmsCapabilities.ClientReferenceLookup).Should().BeFalse();
        client.Should().BeAssignableTo<ISmsClientReferenceSender>();
    }

    [Fact]
    public void SmsIr_AndMelipayamak_DoNotAdvertiseClientReference()
    {
        var smsIr = SmsIr(new FakeSmsIrTransport());
        var melipayamak = Melipayamak(new FakeMelipayamakTransport());

        smsIr.Supports(SmsCapabilities.ClientReference).Should().BeFalse();
        smsIr.Supports(SmsCapabilities.ClientReferenceLookup).Should().BeFalse();
        smsIr.Should().NotBeAssignableTo<ISmsClientReferenceSender>();

        melipayamak.Supports(SmsCapabilities.ClientReference).Should().BeFalse();
        melipayamak.Supports(SmsCapabilities.ClientReferenceLookup).Should().BeFalse();
        melipayamak.Should().NotBeAssignableTo<ISmsClientReferenceSender>();
    }

    [Fact]
    public async Task Kavenegar_SendsNumericLocalId()
    {
        var transport = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"messageid\":8792343,\"cost\":120}]}",
        };

        var result = await Kavenegar(transport).SendWithReferenceAsync("09120000000", "hi", "4501122", "100001", TestContext.Current.CancellationToken);

        transport.LastMethod.Should().Be("sms/send");
        transport.LastParameters!["localid"].Should().Be("4501122");
        transport.LastParameters["receptor"].Should().Be("09120000000");
        result.MessageId.Should().Be("8792343");
        result.ClientReferenceId.Should().Be("4501122");
    }

    [Fact]
    public async Task Kavenegar_RejectsNonNumericReference()
    {
        var client = Kavenegar(new FakeKavenegarTransport());

        await FluentActions.Awaiting(() => client.SendWithReferenceAsync("09120000000", "hi", "order-42", null, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentException>()
            .WithParameterName("clientReferenceId");
    }

    [Fact]
    public async Task Kavenegar_RejectsNullAndBlankReference()
    {
        var client = Kavenegar(new FakeKavenegarTransport());

        await FluentActions.Awaiting(() => client.SendWithReferenceAsync("09120000000", "hi", null!, null, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentNullException>();
        await FluentActions.Awaiting(() => client.SendWithReferenceAsync("09120000000", "hi", "  ", null, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Kavenegar_ClientReferenceCanBeUsedForStatusLookup()
    {
        var transport = new FakeKavenegarTransport
        {
            ResponseBody = "{\"return\":{\"status\":200},\"entries\":[{\"messageid\":8792343,\"localid\":4501122,\"status\":10}]}",
        };
        var client = Kavenegar(transport);

        await client.SendWithReferenceAsync("09120000000", "hi", "4501122", null, TestContext.Current.CancellationToken);

        var status = await client.GetMessageStatusAsync(
            MessageIdentifier.ForClientReferenceId("4501122"),
            TestContext.Current.CancellationToken);

        transport.LastMethod.Should().Be("sms/statuslocalmessageid");
        transport.LastParameters!["localid"].Should().Be("4501122");
        status.State.Should().Be(MessageDeliveryState.Delivered);
    }

    [Fact]
    public async Task Ghasedak_SendsOpaqueClientReferenceId()
    {
        var transport = new FakeGhasedakTransport
        {
            PostResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"MessageId\":\"2387933\"}}",
        };

        var result = await Ghasedak(transport).SendWithReferenceAsync("09120000000", "hi", "order-42", "30006500", TestContext.Current.CancellationToken);

        transport.LastEndpoint.Should().Be("SendSingleSMS");
        transport.LastJsonBody.Should().Contain("\"clientReferenceId\":\"order-42\"");
        result.ClientReferenceId.Should().Be("order-42");
    }

    [Fact]
    public async Task Ghasedak_RejectsNullReference()
    {
        var client = Ghasedak(new FakeGhasedakTransport());

        await FluentActions.Awaiting(() => client.SendWithReferenceAsync("09120000000", "hi", null!, null, TestContext.Current.CancellationToken))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Mock_RecordsAndResolvesClientReference()
    {
        var client = new MockSmsClient();

        var result = await client.SendWithReferenceAsync("09120000000", "hi", "order-42", null, TestContext.Current.CancellationToken);

        result.ClientReferenceId.Should().Be("order-42");
        client.Messages.Should().ContainSingle()
            .Which.ClientReferenceId.Should().Be("order-42");

        var status = await client.GetMessageStatusAsync(
            MessageIdentifier.ForClientReferenceId("order-42"),
            TestContext.Current.CancellationToken);

        status.State.Should().Be(MessageDeliveryState.Delivered);
        status.Recipient.Should().Be("09120000000");
    }

    [Fact]
    public async Task Mock_UnknownClientReference_IsUnknownState()
    {
        var client = new MockSmsClient();

        var status = await client.GetMessageStatusAsync(
            MessageIdentifier.ForClientReferenceId("missing"),
            TestContext.Current.CancellationToken);

        status.State.Should().Be(MessageDeliveryState.Unknown);
        status.RawStatus.Should().Be("not-found");
    }
}
