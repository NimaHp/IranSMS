using FluentAssertions;
using IranSms.Providers.SmsIr;
using Xunit;

namespace IranSms.Tests.SmsIr
{
    public class SmsIrClientTests
    {
        private const string ApiKey = "test-api-key";
        private static readonly string[] TwoRecipients = { "09120000000", "09120000001" };
        private static readonly string[] EmptyRecipients = Array.Empty<string>();
        private static readonly string[] TwoHundredRecipients = BuildMany(200);

        private static string[] BuildMany(int count)
        {
            var list = new string[count];
            for (var i = 0; i < count; i++)
                list[i] = "09120000000";
            return list;
        }

        private static SmsIrClient CreateClient(FakeSmsIrTransport transport)
            => new SmsIrClient(transport, ApiKey);

        private static string OkData(string dataJson)
            => $"{{\"status\":1,\"message\":\"موفق\",\"data\":{dataJson}}}";

        [Fact]
        public void Constructor_Throws_WhenApiKeyNull()
        {
            SmsIrClient? client = null;
            Action act = () => client = new SmsIrClient(null!);
            act.Should().Throw<ArgumentNullException>();
            client.Should().BeNull();
        }

        [Fact]
        public void Constructor_Throws_WhenApiKeyEmpty()
        {
            SmsIrClient? client = null;
            Action act = () => client = new SmsIrClient("");
            act.Should().Throw<ArgumentException>();
            client.Should().BeNull();
        }

        [Fact]
        public async Task SendAsync_GoesThroughBulk_WithSingleRecipient()
        {
            var transport = new FakeSmsIrTransport { ResponseBody = OkData("{\"packId\":\"2b99e63c-9bf8-4a21-9bfe-3f72dc1b46f1\",\"messageIds\":[987],\"cost\":10}") };
            var client = CreateClient(transport);

            var result = await client.SendAsync("09120000000", "hello", "5000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("987");
            transport.LastPath.Should().Be("send/bulk");
        }

        [Fact]
        public async Task SendAsync_UsesDocumentedBody_Types()
        {
            var transport = new FakeSmsIrTransport { ResponseBody = OkData("{\"packId\":\"2b99e63c-9bf8-4a21-9bfe-3f72dc1b46f1\",\"messageIds\":[1]}") };
            var client = CreateClient(transport);

            await client.SendAsync("09120000000", "hello", "5000", TestContext.Current.CancellationToken);

            transport.LastJson!.Should().Contain("\"lineNumber\":5000");
            transport.LastJson.Should().Contain("\"messageText\":\"hello\"");
            transport.LastJson.Should().Contain("\"mobiles\":[\"09120000000\"]");
        }

        [Fact]
        public async Task SendAsync_Throws_WhenSenderLineNull()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendAsync_Throws_WhenSenderLineNotNumeric()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", "abcd", TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendBulkAsync_SendsMobilesArray()
        {
            var transport = new FakeSmsIrTransport
            {
                ResponseBody = OkData("{\"packId\":\"2b99e63c-9bf8-4a21-9bfe-3f72dc1b46f1\",\"messageIds\":[86522023,86522024],\"cost\":2.0}"),
            };
            var client = CreateClient(transport);

            var result = await client.SendBulkAsync(TwoRecipients, "bulk", "5000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("86522023");
            result.RecipientIds.Should().Equal("86522023", "86522024");
            result.Cost.Should().Be(2.0m);
            transport.LastPath.Should().Be("send/bulk");
            transport.LastJson!.Should().Contain("\"mobiles\":[\"09120000000\",\"09120000001\"]");
        }

        [Fact]
        public async Task SendBulkAsync_UsesPackId_WhenMessageIdsEmpty()
        {
            var transport = new FakeSmsIrTransport { ResponseBody = OkData("{\"packId\":\"2b99e63c-9bf8-4a21-9bfe-3f72dc1b46f1\",\"messageIds\":[],\"cost\":0}") };
            var client = CreateClient(transport);

            var result = await client.SendBulkAsync(TwoRecipients, "bulk", "5000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("2b99e63c-9bf8-4a21-9bfe-3f72dc1b46f1");
            result.RecipientIds.Should().BeEmpty();
        }

        [Fact]
        public async Task SendBulkAsync_Throws_Over100Recipients()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            Func<Task> act = async () => await client.SendBulkAsync(TwoHundredRecipients, "x", "5000", TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenEmpty()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            Func<Task> act = async () => await client.SendBulkAsync(EmptyRecipients, "x", "5000", TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Otp_WithCode_UsesCodeParameter()
        {
            var transport = new FakeSmsIrTransport { ResponseBody = OkData("{\"messageId\":42,\"cost\":1.0}") };
            var client = CreateClient(transport);

            var result = await client.SendOtpAsync(
                "09120000000",
                new OtpRequest { TemplateId = "123456", Code = "12345" },
                TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("42");
            result.Cost.Should().Be(1.0m);
            transport.LastPath.Should().Be("send/verify");
            transport.LastJson!.Should().Contain("\"templateId\":123456");
            transport.LastJson.Should().Contain("\"name\":\"Code\"");
            transport.LastJson.Should().Contain("\"value\":\"12345\"");
        }

        [Fact]
        public async Task Otp_WithParameters_SendsNamedParameters()
        {
            var transport = new FakeSmsIrTransport { ResponseBody = OkData("{\"messageId\":42,\"cost\":1.0}") };
            var client = CreateClient(transport);

            await client.SendOtpAsync(
                "09120000000",
                new OtpRequest
                {
                    TemplateId = "123456",
                    Parameters = new Dictionary<string, string> { ["Code"] = "777", ["Name"] = "Ali" },
                },
                TestContext.Current.CancellationToken);

            transport.LastJson!.Should().Contain("\"name\":\"Code\"");
            transport.LastJson.Should().Contain("\"value\":\"777\"");
            transport.LastJson.Should().Contain("\"name\":\"Name\"");
        }

        [Fact]
        public async Task Otp_MissingTemplateId_Throws()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            Func<Task> act = async () => await client.SendOtpAsync("09120000000", new OtpRequest { Code = "123" }, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Otp_NonNumericTemplateId_Throws()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            Func<Task> act = async () => await client.SendOtpAsync(
                "09120000000",
                new OtpRequest { TemplateId = "not-a-number", Code = "123" },
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetMessageStatusAsync_ParsesDocumentedData()
        {
            var transport = new FakeSmsIrTransport
            {
                ResponseBody = OkData(
                    "{\"messageId\":99,\"mobile\":912000000,\"messageText\":\"hi\",\"sendDateTime\":1628683626," +
                    "\"lineNumber\":5000,\"cost\":20,\"deliveryState\":1,\"deliveryDateTime\":1628683629}"),
            };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("99", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Delivered);
            result.RawStatus.Should().Be("1");
            result.Recipient.Should().Be("912000000");
            result.Price.Should().Be(20);
            result.MessageText.Should().Be("hi");
            result.SendDate.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1628683626));
            transport.LastPath.Should().Be("send/99");
        }

        [Fact]
        public async Task GetMessageStatusAsync_LocalId_Throws()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            Func<Task> act = async () => await client.GetMessageStatusAsync(
                new MessageIdentifier("abc", MessageIdentifierType.ClientReferenceId),
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Theory]
        [InlineData("0", MessageDeliveryState.Queued)]
        [InlineData("1", MessageDeliveryState.Delivered)]
        [InlineData("2", MessageDeliveryState.Undelivered)]
        [InlineData("3", MessageDeliveryState.SentToOperator)]
        [InlineData("4", MessageDeliveryState.Undelivered)]
        [InlineData("5", MessageDeliveryState.SentToOperator)]
        [InlineData("6", MessageDeliveryState.Failed)]
        [InlineData("7", MessageDeliveryState.Blocked)]
        public async Task StatusMapping_VariousValues(string raw, MessageDeliveryState expected)
        {
            var transport = new FakeSmsIrTransport
            {
                ResponseBody = OkData("{\"messageId\":1,\"deliveryState\":" + raw + "}"),
            };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(expected);
        }

        [Fact]
        public async Task ApiError_Throws_WithCode()
        {
            var transport = new FakeSmsIrTransport
            {
                ResponseBody = "{\"status\":0,\"message\":\"نامعتبر\",\"data\":null}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", "5000", TestContext.Current.CancellationToken);
            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.ProviderStatusCode.Should().Be(0);
            ex.ProviderName.Should().Be("SmsIr");
        }

        [Fact]
        public void Capabilities_AreCorrect()
        {
            var client = CreateClient(new FakeSmsIrTransport());
            client.Capabilities.Should().Be(
                SmsCapabilities.Send | SmsCapabilities.BulkSend | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus);
            client.ProviderName.Should().Be("SmsIr");
        }
    }
}
