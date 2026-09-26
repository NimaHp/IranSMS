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
/// The normalized delivery-state matrix per provider, verified against each provider's
/// official status table. Kavenegar exposes the complete official set of ten codes,
/// Ghasedak the complete 0-8 range, SMS.ir only the documented 1-7, and Melipayamak the
/// documented <c>GetDeliveries</c> return values.
/// </summary>
public class DeliveryStateMatrixTests
{
    private const string KavenegarEnvelopePrefix = "{\"return\":{\"status\":200},\"entries\":[";

    private static KavenegarClient Kavenegar(string status)
        => new KavenegarClient(
            new FakeKavenegarTransport
            {
                ResponseBody = KavenegarEnvelopePrefix + "{\"messageid\":1,\"status\":" + status + "}]}",
            },
            "test-api-key");

    private static GhasedakClient Ghasedak(int status)
        => new GhasedakClient(
            new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":[{\"Status\":" + status + "}]}",
            },
            "test-api-key");

    private static SmsIrClient SmsIr(int? deliveryState)
        => new SmsIrClient(
            new FakeSmsIrTransport
            {
                ResponseBody = "{\"status\":1,\"data\":{\"messageId\":1,\"deliveryState\":" +
                    (deliveryState.HasValue
                        ? deliveryState.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                        : "null") + "}}",
            },
            "test-api-key");

    private static MelipayamakClient Melipayamak(string status)
        => new MelipayamakClient(new FakeMelipayamakTransport { ResponseBody = status }, "user", "pass");

    [Theory]
    [InlineData("1", MessageDeliveryState.Queued)]
    [InlineData("2", MessageDeliveryState.Scheduled)]
    [InlineData("4", MessageDeliveryState.SentToOperator)]
    [InlineData("5", MessageDeliveryState.SentToOperator)]
    [InlineData("6", MessageDeliveryState.Failed)]
    [InlineData("10", MessageDeliveryState.Delivered)]
    [InlineData("11", MessageDeliveryState.Undelivered)]
    [InlineData("13", MessageDeliveryState.Cancelled)]
    [InlineData("14", MessageDeliveryState.Blocked)]
    [InlineData("100", MessageDeliveryState.Unknown)]
    [InlineData("3", MessageDeliveryState.Unknown)]
    [InlineData("42", MessageDeliveryState.Unknown)]
    public async Task Kavenegar_CoversTheCompleteOfficialTable(string raw, MessageDeliveryState expected)
    {
        var result = await Kavenegar(raw).GetMessageStatusAsync(
            MessageIdentifier.ForProviderMessageId("1"),
            TestContext.Current.CancellationToken);

        result.State.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, MessageDeliveryState.Unknown)]
    [InlineData(1, MessageDeliveryState.Cancelled)]
    [InlineData(2, MessageDeliveryState.Blocked)]
    [InlineData(3, MessageDeliveryState.SentToOperator)]
    [InlineData(4, MessageDeliveryState.Undelivered)]
    [InlineData(5, MessageDeliveryState.Delivered)]
    [InlineData(6, MessageDeliveryState.Failed)]
    [InlineData(7, MessageDeliveryState.Unknown)]
    [InlineData(8, MessageDeliveryState.Unknown)]
    [InlineData(9, MessageDeliveryState.Unknown)]
    public async Task Ghasedak_CoversTheCompleteOfficialRange(int status, MessageDeliveryState expected)
    {
        var result = await Ghasedak(status).GetMessageStatusAsync(
            MessageIdentifier.ForProviderMessageId("1"),
            TestContext.Current.CancellationToken);

        result.State.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, MessageDeliveryState.Delivered)]
    [InlineData(2, MessageDeliveryState.Undelivered)]
    [InlineData(3, MessageDeliveryState.SentToOperator)]
    [InlineData(4, MessageDeliveryState.Failed)]
    [InlineData(5, MessageDeliveryState.SentToOperator)]
    [InlineData(6, MessageDeliveryState.Failed)]
    [InlineData(7, MessageDeliveryState.Blocked)]
    [InlineData(0, MessageDeliveryState.Unknown)]
    [InlineData(null, MessageDeliveryState.Unknown)]
    public async Task SmsIr_CoversTheDocumentedRange(int? status, MessageDeliveryState expected)
    {
        var result = await SmsIr(status).GetMessageStatusAsync(
            MessageIdentifier.ForProviderMessageId("1"),
            TestContext.Current.CancellationToken);

        result.State.Should().Be(expected);
    }

    [Theory]
    [InlineData("-1", MessageDeliveryState.Failed)]
    [InlineData("0", MessageDeliveryState.SentToOperator)]
    [InlineData("1", MessageDeliveryState.Delivered)]
    [InlineData("2", MessageDeliveryState.Undelivered)]
    [InlineData("3", MessageDeliveryState.Failed)]
    [InlineData("5", MessageDeliveryState.Failed)]
    [InlineData("8", MessageDeliveryState.SentToOperator)]
    [InlineData("16", MessageDeliveryState.Failed)]
    [InlineData("35", MessageDeliveryState.Blocked)]
    [InlineData("100", MessageDeliveryState.Unknown)]
    [InlineData("200", MessageDeliveryState.SentToOperator)]
    [InlineData("300", MessageDeliveryState.Blocked)]
    [InlineData("400", MessageDeliveryState.Queued)]
    [InlineData("500", MessageDeliveryState.Failed)]
    public async Task Melipayamak_CoversTheDocumentedReturnValues(string status, MessageDeliveryState expected)
    {
        var result = await Melipayamak(status).GetMessageStatusAsync(
            MessageIdentifier.ForProviderMessageId("1"),
            TestContext.Current.CancellationToken);

        result.State.Should().Be(expected);
    }

    [Theory]
    [InlineData("-2")]
    [InlineData("-3")]
    [InlineData("-10")]
    [InlineData("-108")]
    [InlineData("-109")]
    [InlineData("-110")]
    public async Task Melipayamak_ApiLevelCodes_StayUnknownButKeepRawValue(string status)
    {
        var result = await Melipayamak(status).GetMessageStatusAsync(
            MessageIdentifier.ForProviderMessageId("1"),
            TestContext.Current.CancellationToken);

        result.State.Should().Be(MessageDeliveryState.Unknown);
        result.RawStatus.Should().Be(status);
    }
}
