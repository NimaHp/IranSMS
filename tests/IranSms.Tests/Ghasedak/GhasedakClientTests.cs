using FluentAssertions;
using IranSms.Providers.Ghasedak;
using Xunit;

namespace IranSms.Tests.Ghasedak
{
    public class GhasedakClientTests
    {
        private const string ApiKey = "test-key";
        private static readonly string[] TwoRecipients = { "09120000000", "09120000001" };
        private static readonly string[] PaddedRecipient = { " 09120000000 " };
        private static readonly string[] WhitespaceRecipient = { " \t " };
        private static readonly string[] EmptyRecipients = Array.Empty<string>();
        private static readonly string[] Over100Recipients = CreateMany(101);

        private static GhasedakClient CreateClient(FakeGhasedakTransport transport)
            => new GhasedakClient(transport, ApiKey);

        private static string[] CreateMany(int n)
        {
            var arr = new string[n];
            for (var i = 0; i < n; i++)
                arr[i] = "0912" + i.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(7, '0');
            return arr;
        }

        [Fact]
        public void Constructor_Throws_WhenApiKeyNull()
        {
            GhasedakClient? client = null;
            Action act = () => client = new GhasedakClient(null!);
            act.Should().Throw<ArgumentNullException>();
            client.Should().BeNull();
        }

        [Fact]
        public void Constructor_Throws_WhenApiKeyEmpty()
        {
            GhasedakClient? client = null;
            Action act = () => client = new GhasedakClient("");
            act.Should().Throw<ArgumentException>();
            client.Should().BeNull();
        }

        [Fact]
        public async Task SendAsync_PostsSingle_AndParsesMessageId()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"MessageId\":\"gh-1\"}}",
            };
            var client = CreateClient(transport);

            var result = await client.SendAsync("09120000000", "hi", "3000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("gh-1");
            transport.LastEndpoint.Should().Be("SendSingleSMS");
            transport.LastJsonBody.Should().Contain("\"receptor\":\"09120000000\"");
            transport.LastJsonBody.Should().Contain("\"message\":\"hi\"");
            transport.LastJsonBody.Should().Contain("\"lineNumber\":\"3000\"");
        }

        [Fact]
        public async Task SendAsync_TrimsRecipient_AndRejectsWhitespace()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"MessageId\":\"gh-1\"}}",
            };
            var client = CreateClient(transport);

            await client.SendAsync(" 09120000000 ", "hi", null, TestContext.Current.CancellationToken);
            transport.LastJsonBody.Should().Contain("\"receptor\":\"09120000000\"");

            Func<Task> act = async () => await client.SendAsync(" \t ", "hi", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendAsync_Accepts1000Characters_AndRejectsLongerMessage()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"MessageId\":\"gh-1\"}}",
            };
            var client = CreateClient(transport);

            await client.SendAsync("09120000000", new string('x', 1000), null, TestContext.Current.CancellationToken);
            transport.PostCount.Should().Be(1);

            Func<Task> act = async () => await client.SendAsync("09120000000", new string('x', 1001), null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
            transport.PostCount.Should().Be(1);
        }

        [Fact]
        public async Task SendAsync_MissingMessageId_Throws()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"Data\":{},\"IsSuccess\":true,\"StatusCode\":200}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);

            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.ProviderName.Should().Be("Ghasedak");
            ex.RawResponseBody.Should().Be(transport.PostResponse);
        }

        [Fact]
        public async Task SendAsync_ErrorEnvelope_Throws()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"IsSuccess\":false,\"StatusCode\":418,\"Message\":\"?????? ???? ????\"}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);
            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.ProviderStatusCode.Should().Be(418);
            ex.Message.Should().NotContain("?????? ???? ????");
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("\"ok\"")]
        [InlineData("{\"IsSuccess\":\"true\",\"StatusCode\":200,\"Data\":{}}")]
        [InlineData("{\"IsSuccess\":true,\"StatusCode\":{},\"Data\":{}}")]
        [InlineData("{\"IsSuccess\":true,\"StatusCode\":200,\"Message\":42,\"Data\":{}}")]
        public async Task SendAsync_MalformedEnvelope_ThrowsIranSmsException(string response)
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = response,
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);

            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.ProviderName.Should().Be("Ghasedak");
            ex.RawResponseBody.Should().Be(response);
        }

        [Fact]
        public async Task SendBulkAsync_JoinsRecipients_AndUsesBulkEndpoint()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"Data\":{\"Cost\":3537,\"LineNumber\":\"3000\",\"Receptors\":[{\"Receptor\":\"09120000000\",\"MessageId\":\"4248\"},{\"Receptor\":\"09120000001\",\"MessageId\":\"4249\"}]},\"IsSuccess\":true,\"StatusCode\":200,\"Message\":\"ok\"}",
            };
            var client = CreateClient(transport);

            var result = await client.SendBulkAsync(TwoRecipients, "bulk", "3000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("4248");
            result.RecipientIds.Should().Equal("4248", "4249");
            transport.LastEndpoint.Should().Be("SendBulkSMS");
            transport.LastJsonBody.Should().Contain("\"receptors\":[\"09120000000\",\"09120000001\"]");
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenResponseMessageIdIsMissing()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"Data\":{\"Receptors\":[{\"Receptor\":\"09120000000\"}]},\"IsSuccess\":true,\"StatusCode\":200}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendBulkAsync(TwoRecipients, "bulk", null, TestContext.Current.CancellationToken);

            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.ProviderName.Should().Be("Ghasedak");
            ex.RawResponseBody.Should().Be(transport.PostResponse);
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenEmpty()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            Func<Task> act = async () => await client.SendBulkAsync(EmptyRecipients, "x", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendBulkAsync_TrimsRecipients_AndRejectsWhitespace()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"Data\":{\"Receptors\":[{\"MessageId\":\"gh-b\"}]},\"IsSuccess\":true,\"StatusCode\":200}",
            };
            var client = CreateClient(transport);

            await client.SendBulkAsync(PaddedRecipient, "bulk", null, TestContext.Current.CancellationToken);
            transport.LastJsonBody.Should().Contain("\"receptors\":[\"09120000000\"]");

            Func<Task> act = async () => await client.SendBulkAsync(WhitespaceRecipient, "bulk", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenOver100Recipients()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            Func<Task> act = async () => await client.SendBulkAsync(Over100Recipients, "x", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenMessageTooLong()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            Func<Task> act = async () => await client.SendBulkAsync(TwoRecipients, new string('x', 1001), null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Otp_SendsTemplateAndParams()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"Data\":{\"LineNumber\":\"10002000200101\",\"MessageBody\":\"code\",\"Items\":[{\"Receptor\":\"09120000000\",\"Cost\":940,\"MessageId\":\"2387931\"}],\"Cost\":940},\"IsSuccess\":true,\"StatusCode\":200,\"Message\":\"ok\"}",
            };
            var client = CreateClient(transport);

            var result = await client.SendOtpAsync(
                "09120000000",
                new OtpTemplateRequest("verify")
                    .SetParameter("param1", "12345")
                    .SetParameter("param10", "ten"),
                TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("2387931");
            transport.LastEndpoint.Should().Be("SendOtpWithParams");
            transport.LastJsonBody.Should().Contain("\"templateName\":\"verify\"");
            transport.LastJsonBody.Should().Contain("\"receptors\":[{\"mobile\":\"09120000000\"}]");
            transport.LastJsonBody.Should().Contain("\"param1\":\"12345\"");
            transport.LastJsonBody.Should().Contain("\"param10\":\"ten\"");
            transport.LastJsonBody.Should().NotContain("\"inputs\"");
        }

        [Fact]
        public async Task Otp_WithCode_UsesParam1()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"Data\":{\"Items\":[{\"MessageId\":\"2387931\"}]},\"IsSuccess\":true,\"StatusCode\":200}",
            };
            var client = CreateClient(transport);

            await client.SendOtpAsync(
                "09120000000",
                new OtpTemplateRequest("verify").SetParameter("param1", "12345"),
                TestContext.Current.CancellationToken);

            transport.LastJsonBody.Should().Contain("\"param1\":\"12345\"");
            transport.LastJsonBody.Should().NotContain("\"inputs\"");
        }

        [Fact]
        public async Task Otp_ResponseMessageIdIsMissing_Throws()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"Data\":{\"Items\":[{\"Receptor\":\"09120000000\"}]},\"IsSuccess\":true,\"StatusCode\":200}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendOtpAsync(
                "09120000000",
                new OtpTemplateRequest("verify").SetParameter("param1", "12345"),
                TestContext.Current.CancellationToken);

            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.RawResponseBody.Should().Be(transport.PostResponse);
        }

        [Fact]
        public async Task Otp_RejectsWhitespaceAndMissingParam1()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            var request = new OtpTemplateRequest("verify");

            Func<Task> whitespace = async () => await client.SendOtpAsync(" \t ", request, TestContext.Current.CancellationToken);
            await whitespace.Should().ThrowAsync<ArgumentException>();

            request.Parameters = new Dictionary<string, string>();
            Func<Task> missingParam = async () => await client.SendOtpAsync("09120000000", request, TestContext.Current.CancellationToken);
            await missingParam.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Otp_MissingTemplate_Throws()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            Func<Task> act = async () => await client.SendOtpAsync(
                "09120000000",
                new OtpCodeRequest("12345"),
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetMessageStatusAsync_ParsesDelivered()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":[{\"MessageId\":\"1\",\"Receptor\":\"09120000000\",\"Message\":\"hi\",\"Status\":5}]}",
            };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Delivered);
            result.RawStatus.Should().Be("5");
            result.Recipient.Should().Be("09120000000");
            result.MessageText.Should().Be("hi");
            transport.LastEndpoint.Should().Be("CheckSmsStatus");
            transport.LastQuery!["Ids"].Should().Be("1");
            transport.LastQuery["Type"].Should().Be("1");
        }

        [Fact]
        public async Task GetMessageStatusAsync_ParsesStringStatus()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":\"200\",\"Data\":[{\"Status\":\"5\"}]}",
            };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Delivered);
            result.RawStatus.Should().Be("5");
        }

        [Fact]
        public async Task GetMessageStatusAsync_MalformedData_ThrowsIranSmsException()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{}}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            await act.Should().ThrowAsync<IranSmsException>();
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
        public async Task GetMessageStatusAsync_MapsCodes(int code, MessageDeliveryState expected)
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = $"{{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":[{{\"Status\":{code}}}]}}",
            };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(expected);
        }

        [Fact]
        public async Task GetMessageStatusAsync_ClientReference_UsesType2()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":[{\"Status\":0}]}",
            };
            var client = CreateClient(transport);

            await client.GetMessageStatusAsync(
                new MessageIdentifier("ref-1", MessageIdentifierType.ClientReferenceId),
                TestContext.Current.CancellationToken);

            transport.LastQuery!["Type"].Should().Be("2");
            transport.LastQuery["Ids"].Should().Be("ref-1");
        }

        [Fact]
        public async Task GetMessageStatusAsync_EmptyData_ReturnsUnknown()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":[]}",
            };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Unknown);
            result.RawStatus.Should().Be("no-data");
        }

        [Fact]
        public async Task GetBalanceAsync_ParsesCredit_Plan_Expire()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"Credit\":9135538,\"ExpireDate\":\"2024-07-16T10:48:21+03:30\",\"Plan\":\"silver\"}}",
            };
            var client = CreateClient(transport);

            var result = await client.GetBalanceAsync(TestContext.Current.CancellationToken);

            result.Credit.Should().Be(9135538m);
            result.AccountType.Should().Be("silver");
            result.ExpireDate.Should().NotBeNull();
            transport.LastEndpoint.Should().Be("GetAccountInformation");
        }

        [Fact]
        public async Task GetBalanceAsync_MissingCredit_Throws()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"Plan\":\"silver\"}}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.GetBalanceAsync(TestContext.Current.CancellationToken);

            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.ProviderName.Should().Be("Ghasedak");
            ex.RawResponseBody.Should().Be(transport.GetResponse);
        }

        [Fact]
        public async Task GetBalanceAsync_ApiError_Throws()
        {
            var transport = new FakeGhasedakTransport
            {
                GetResponse = "{\"IsSuccess\":false,\"StatusCode\":418,\"Message\":\"error\"}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.GetBalanceAsync(TestContext.Current.CancellationToken);
            var ex = (await act.Should().ThrowAsync<IranSmsException>()).Which;
            ex.ProviderStatusCode.Should().Be(418);
        }

        [Fact]
        public async Task GetSenderLinesAsync_ReturnsEmpty_NoEndpoint()
        {
            var client = CreateClient(new FakeGhasedakTransport());

            var lines = await client.GetSenderLinesAsync(TestContext.Current.CancellationToken);

            lines.Should().BeEmpty();
        }

        [Fact]
        public void Capabilities_AreCorrect()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            client.Capabilities.Should().Be(
                SmsCapabilities.Send | SmsCapabilities.BulkSend | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus | SmsCapabilities.AccountInfo | SmsCapabilities.ClientReference);
            client.ProviderName.Should().Be("Ghasedak");
        }
    }
}
