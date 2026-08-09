using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IranianSms.Providers.Ghasedak;
using Xunit;

namespace IranianSms.Tests.Ghasedak
{
    public class GhasedakClientTests
    {
        private const string ApiKey = "test-key";
        private static readonly string[] TwoRecipients = { "09120000000", "09120000001" };
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
        public async Task SendAsync_ErrorEnvelope_Throws()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"IsSuccess\":false,\"StatusCode\":418,\"Message\":\"اعتبار کافی نیست\"}",
            };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);
            var ex = (await act.Should().ThrowAsync<IranianSmsException>()).Which;
            ex.ProviderStatusCode.Should().Be(418);
            ex.Message.Should().Contain("اعتبار کافی نیست");
        }

        [Fact]
        public async Task SendBulkAsync_JoinsRecipients_AndUsesBulkEndpoint()
        {
            var transport = new FakeGhasedakTransport
            {
                PostResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"MessageId\":\"gh-b\"}}",
            };
            var client = CreateClient(transport);

            var result = await client.SendBulkAsync(TwoRecipients, "bulk", "3000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("gh-b");
            transport.LastEndpoint.Should().Be("SendBulkSMS");
            transport.LastJsonBody.Should().Contain("\"receptors\":\"09120000000,09120000001\"");
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenEmpty()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            Func<Task> act = async () => await client.SendBulkAsync(EmptyRecipients, "x", null, TestContext.Current.CancellationToken);
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
                PostResponse = "{\"IsSuccess\":true,\"StatusCode\":200,\"Data\":{\"MessageId\":\"gh-o\"}}",
            };
            var client = CreateClient(transport);

            var result = await client.SendOtpAsync(
                "09120000000",
                new OtpRequest
                {
                    TemplateId = "verify",
                    Parameters = new Dictionary<string, string> { ["Code"] = "12345" },
                },
                TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("gh-o");
            transport.LastEndpoint.Should().Be("SendOtpSMS");
            transport.LastJsonBody.Should().Contain("\"templateName\":\"verify\"");
            transport.LastJsonBody.Should().Contain("\"mobile\":\"09120000000\"");
            transport.LastJsonBody.Should().Contain("\"Code\":\"12345\"");
        }

        [Fact]
        public async Task Otp_MissingTemplate_Throws()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            Func<Task> act = async () => await client.SendOtpAsync(
                "09120000000",
                new OtpRequest(),
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
        public void Capabilities_AreCorrect()
        {
            var client = CreateClient(new FakeGhasedakTransport());
            client.Capabilities.Should().Be(
                SmsCapabilities.Send | SmsCapabilities.BulkSend | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus);
            client.ProviderName.Should().Be("Ghasedak");
        }
    }
}