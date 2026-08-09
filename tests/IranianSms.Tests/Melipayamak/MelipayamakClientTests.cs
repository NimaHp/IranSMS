using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IranianSms.Providers.Melipayamak;
using Xunit;

namespace IranianSms.Tests.Melipayamak
{
    public class MelipayamakClientTests
    {
        private const string Username = "user";
        private const string Password = "pass";
        private static readonly string[] TwoRecipients = { "09120000000", "09120000001" };
        private static readonly string[] EmptyRecipients = Array.Empty<string>();

        private static MelipayamakClient CreateClient(FakeMelipayamakTransport transport)
            => new MelipayamakClient(transport, Username, Password);

        [Fact]
        public void Constructor_Throws_WhenUsernameNull()
        {
            MelipayamakClient? client = null;
            Action act = () => client = new MelipayamakClient(null!, Password);
            act.Should().Throw<ArgumentNullException>();
            client.Should().BeNull();
        }

        [Fact]
        public void Constructor_Throws_WhenPasswordNull()
        {
            MelipayamakClient? client = null;
            Action act = () => client = new MelipayamakClient(Username, null!);
            act.Should().Throw<ArgumentNullException>();
            client.Should().BeNull();
        }

        [Fact]
        public void Constructor_Throws_WhenPasswordEmpty()
        {
            MelipayamakClient? client = null;
            Action act = () => client = new MelipayamakClient(Username, "");
            act.Should().Throw<ArgumentException>();
            client.Should().BeNull();
        }

        [Fact]
        public async Task SendAsync_ParsesRecId()
        {
            var transport = new FakeMelipayamakTransport { ResponseBody = "98765" };
            var client = CreateClient(transport);

            var result = await client.SendAsync("09120000000", "hello", "5000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("98765");
            transport.LastAction.Should().Be("SendSMS");
            transport.LastForm!["to"].Should().Be("09120000000");
            transport.LastForm["from"].Should().Be("5000");
            transport.LastForm["text"].Should().Be("hello");
            transport.LastForm["username"].Should().Be(Username);
            transport.LastForm["password"].Should().Be(Password);
        }

        [Fact]
        public async Task SendAsync_Throws_WhenSenderLineNull()
        {
            var client = CreateClient(new FakeMelipayamakTransport());
            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendAsync_ErrorCode_ThrowsWithMessage()
        {
            var transport = new FakeMelipayamakTransport { ResponseBody = "-110" };
            var client = CreateClient(transport);

            Func<Task> act = async () => await client.SendAsync("09120000000", "hi", "5000", TestContext.Current.CancellationToken);
            var ex = (await act.Should().ThrowAsync<IranianSmsException>()).Which;
            ex.ProviderStatusCode.Should().Be(-110);
            ex.Message.Should().Contain("API key");
        }

        [Fact]
        public async Task SendBulkAsync_JoinsRecipients_AndUsesBulkEndpoint()
        {
            var transport = new FakeMelipayamakTransport { ResponseBody = "123" };
            var client = CreateClient(transport);

            var result = await client.SendBulkAsync(TwoRecipients, "bulk", "5000", TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("123");
            transport.LastAction.Should().Be("SendBulkSMS");
            transport.LastForm!["to"].Should().Be("09120000000,09120000001");
        }

        [Fact]
        public async Task SendBulkAsync_Throws_WhenEmpty()
        {
            var client = CreateClient(new FakeMelipayamakTransport());
            Func<Task> act = async () => await client.SendBulkAsync(EmptyRecipients, "x", "5000", TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Otp_SendsCode()
        {
            var transport = new FakeMelipayamakTransport { ResponseBody = "555" };
            var client = CreateClient(transport);

            var result = await client.SendOtpAsync(
                "09120000000",
                new OtpRequest { Code = "12345", SenderLine = "5000" },
                TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("555");
            transport.LastAction.Should().Be("SendOtp");
            transport.LastForm!["code"].Should().Be("12345");
        }

        [Fact]
        public async Task Otp_MissingCode_Throws()
        {
            var client = CreateClient(new FakeMelipayamakTransport());
            Func<Task> act = async () => await client.SendOtpAsync(
                "09120000000",
                new OtpRequest { SenderLine = "5000" },
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task Otp_MissingSenderLine_Throws()
        {
            var client = CreateClient(new FakeMelipayamakTransport());
            Func<Task> act = async () => await client.SendOtpAsync(
                "09120000000",
                new OtpRequest { Code = "123" },
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetMessageStatusAsync_ParsesDelivery()
        {
            var transport = new FakeMelipayamakTransport { ResponseBody = "1" };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("98765", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Delivered);
            result.RawStatus.Should().Be("1");
            transport.LastAction.Should().Be("GetDelivery");
            transport.LastForm!["recId"].Should().Be("98765");
        }

        [Theory]
        [InlineData("-1", MessageDeliveryState.Failed)]
        [InlineData("0", MessageDeliveryState.Delivered)]
        [InlineData("1", MessageDeliveryState.Delivered)]
        [InlineData("2", MessageDeliveryState.SentToOperator)]
        [InlineData("5", MessageDeliveryState.Queued)]
        [InlineData("16", MessageDeliveryState.Failed)]
        [InlineData("99", MessageDeliveryState.Unknown)]
        public async Task GetMessageStatusAsync_MapsCodes(string raw, MessageDeliveryState expected)
        {
            var transport = new FakeMelipayamakTransport { ResponseBody = raw };
            var client = CreateClient(transport);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(expected);
        }

        [Fact]
        public async Task GetMessageStatusAsync_LocalId_Throws()
        {
            var client = CreateClient(new FakeMelipayamakTransport());
            Func<Task> act = async () => await client.GetMessageStatusAsync(
                new MessageIdentifier("abc", MessageIdentifierType.ClientReferenceId),
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetMessageStatusAsync_NonNumericRecId_Throws()
        {
            var client = CreateClient(new FakeMelipayamakTransport());
            Func<Task> act = async () => await client.GetMessageStatusAsync(
                new MessageIdentifier("not-a-number", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void Capabilities_AreCorrect()
        {
            var client = CreateClient(new FakeMelipayamakTransport());
            client.Capabilities.Should().Be(
                SmsCapabilities.Send | SmsCapabilities.BulkSend | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus);
            client.ProviderName.Should().Be("Melipayamak");
        }
    }
}