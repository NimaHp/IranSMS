using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IranianSms.Providers.Kavenegar;
using Xunit;

namespace IranianSms.Tests.Kavenegar
{
    public class KavenegarClientTests
    {
        private const string ApiKey = "test-api-key";
        private static readonly string[] TwoRecipients = { "09120000000", "09120000001" };
        private static readonly string[] EmptyRecipients = Array.Empty<string>();
        private static readonly string[] ExpectedRecipientIds = { "1", "2" };

        private static KavenegarClient CreateClient(FakeKavenegarTransport transport)
            => new KavenegarClient(transport, ApiKey);

        private static string Envelope(string entriesJson)
            => $"{{\"return\":{{\"status\":200,\"message\":\"OK\"}},\"entries\":[{entriesJson}]}}";

        private static string SendEntryJson(long messageId, decimal cost, int status = 1)
            => $"{{\"messageid\":{messageId},\"message\":\"hi\",\"status\":{status},\"statustext\":\"queued\",\"sender\":\"100001\",\"receptor\":\"09120000000\",\"date\":1720000000,\"cost\":{cost}}}";

        [Fact]
        public void Constructor_Throws_WhenApiKeyNull()
        {
            KavenegarClient? client = null;
            Action act = () => client = new KavenegarClient(null!);
            act.Should().Throw<ArgumentNullException>();
            client.Should().BeNull();
        }

        [Fact]
        public void Constructor_Throws_WhenApiKeyEmpty()
        {
            KavenegarClient? client = null;
            Action act = () => client = new KavenegarClient("");
            act.Should().Throw<ArgumentException>();
            client.Should().BeNull();
        }

        [Fact]
        public async Task SendAsync_ParsesMessageId_AndCost()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(123456, 42)) };
            var client = CreateClient(transport);

            var result = await client.SendAsync("09120000000", "hello", "100001", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("123456");
            result.Cost.Should().Be(42);
            transport.LastMethod.Should().Be("sms/send");
        }

        [Fact]
        public async Task SendAsync_SendsFormParameters()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(1, 0)) };
            var client = CreateClient(transport);

            await client.SendAsync("09120000000", "hello", "100001", TestContext.Current.CancellationToken);

            transport.LastParameters!["receptor"].Should().Be("09120000000");
            transport.LastParameters!["message"].Should().Be("hello");
            transport.LastParameters!["sender"].Should().Be("100001");
        }

        [Fact]
        public async Task SendAsync_OmittedSender_DoesNotSendSenderParam()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(1, 0)) };
            var client = CreateClient(transport);

            await client.SendAsync("09120000000", "hello", null, TestContext.Current.CancellationToken);

            transport.LastParameters!.ContainsKey("sender").Should().BeFalse();
        }

        [Fact]
        public async Task SendBulkAsync_JoinsReceptors_AndReturnsRecipientIds()
        {
            var transport = new FakeKavenegarTransport
            {
                ResponseBody = "{\"return\":{\"status\":200,\"message\":\"OK\"},\"entries\":[" +
                    SendEntryJson(1, 0) + "," + SendEntryJson(2, 0) + "]}",
            };
            var client = CreateClient(transport);

            var result = await client.SendBulkAsync(TwoRecipients, "bulk", "100001", TestContext.Current.CancellationToken);

            transport.LastMethod.Should().Be("sms/send");
            transport.LastParameters!["receptor"].Should().Be("09120000000,09120000001");
            result.MessageId.Should().Be("1");
            result.RecipientIds.Should().BeEquivalentTo(ExpectedRecipientIds);
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenEmpty()
        {
            var client = CreateClient(new FakeKavenegarTransport());
            Func<Task> act = async () => await client.SendBulkAsync(EmptyRecipients, "x", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenOver200Recipients()
        {
            var client = CreateClient(new FakeKavenegarTransport());
            var many = new List<string>();
            for (var i = 0; i < 201; i++)
                many.Add("09120000000");
            Func<Task> act = async () => await client.SendBulkAsync(many, "x", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Otp_WithCode_UsesToken()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(55, 10)) };
            var client = CreateClient(transport);

            var result = await client.SendOtpAsync(
                "09220000000",
                new OtpRequest { TemplateId = "verify", Code = "12345" },
                TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("55");
            transport.LastMethod.Should().Be("verify/lookup");
            transport.LastParameters!["template"].Should().Be("verify");
            transport.LastParameters!["token"].Should().Be("12345");
        }

        [Fact]
        public async Task Otp_WithParameterTokens_CopiesToForm()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(55, 10)) };
            var client = CreateClient(transport);

            await client.SendOtpAsync(
                "09220000000",
                new OtpRequest
                {
                    TemplateId = "verify",
                    Parameters = new Dictionary<string, string> { ["token"] = "111", ["token2"] = "222" },
                },
                TestContext.Current.CancellationToken);

            transport.LastParameters!["token"].Should().Be("111");
            transport.LastParameters!["token2"].Should().Be("222");
        }

        [Fact]
        public async Task Otp_MissingCodeAndParams_Throws()
        {
            var client = CreateClient(new FakeKavenegarTransport());
            Func<Task> act = async () => await client.SendOtpAsync(
                "09220000000",
                new OtpRequest { TemplateId = "verify" },
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetMessageStatusAsync_ProviderId_MapsDelivered()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(100, 1, status: 10)) };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("100", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Delivered);
            result.RawStatus.Should().Be("10");
            transport.LastMethod.Should().Be("sms/status");
            transport.LastParameters!["messageid"].Should().Be("100");
        }

        [Theory]
        [InlineData(1, MessageDeliveryState.Queued)]
        [InlineData(2, MessageDeliveryState.Scheduled)]
        [InlineData(4, MessageDeliveryState.SentToOperator)]
        [InlineData(5, MessageDeliveryState.SentToOperator)]
        [InlineData(6, MessageDeliveryState.Failed)]
        [InlineData(10, MessageDeliveryState.Delivered)]
        [InlineData(11, MessageDeliveryState.Undelivered)]
        [InlineData(13, MessageDeliveryState.Cancelled)]
        [InlineData(14, MessageDeliveryState.Blocked)]
        [InlineData(100, MessageDeliveryState.Unknown)]
        [InlineData(999, MessageDeliveryState.Unknown)]
        public async Task StatusMapping_AllCodes(int code, MessageDeliveryState expected)
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(1, 0, status: code)) };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(expected);
            result.RawStatus.Should().Be(code.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task StatusAsync_LocalId_UsesLocalEndpoint()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = Envelope(SendEntryJson(1, 0)) };
            var client = CreateClient(transport);

            await client.GetMessageStatusAsync(
                new MessageIdentifier("loc-42", MessageIdentifierType.ClientReferenceId),
                TestContext.Current.CancellationToken);

            transport.LastMethod.Should().Be("sms/statuslocalmessageid");
            transport.LastParameters!["localid"].Should().Be("loc-42");
        }

        [Fact]
        public async Task ApiError_Throws_WithCode()
        {
            var transport = new FakeKavenegarTransport
            {
                ResponseBody = "{\"return\":{\"status\":418,\"message\":\"Insufficient credit\"},\"entries\":[]}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);
            var ex = (await act.Should().ThrowAsync<IranianSmsException>()).Which;
            ex.ProviderStatusCode.Should().Be(418);
            ex.ProviderName.Should().Be("Kavenegar");
        }

        [Fact]
        public async Task MalformedResponse_Throws()
        {
            var transport = new FakeKavenegarTransport { ResponseBody = "not-json" };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<IranianSmsException>();
        }

        [Fact]
        public void Capabilities_AreCorrect()
        {
            var client = CreateClient(new FakeKavenegarTransport());
            client.Capabilities.Should().Be(
                SmsCapabilities.Send | SmsCapabilities.BulkSend | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus);
            client.ProviderName.Should().Be("Kavenegar");
        }
    }
}