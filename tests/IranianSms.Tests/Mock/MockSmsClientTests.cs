using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using IranianSms.Providers.Mock;
using Xunit;

namespace IranianSms.Tests.Mock
{
    public class MockSmsClientTests
    {
        private static MockSmsClient CreateClient() => new MockSmsClient();

        [Fact]
        public void Constructor_DefaultName_IsMock()
        {
            var client = new MockSmsClient();
            client.ProviderName.Should().Be("Mock");
        }

        [Fact]
        public void Constructor_CustomName_IsUsed()
        {
            var client = new MockSmsClient("Fake");
            client.ProviderName.Should().Be("Fake");
        }

        [Fact]
        public void Capabilities_AreCorrect()
        {
            var client = CreateClient();
            client.Capabilities.Should().Be(
                SmsCapabilities.Send | SmsCapabilities.BulkSend | SmsCapabilities.OtpSend | SmsCapabilities.DeliveryStatus);
        }

        [Fact]
        public async Task SendAsync_RecordsMessage_AndReturnsSequentialId()
        {
            var client = CreateClient();

            var result1 = await client.SendAsync("09120000000", "hello", "3000", TestContext.Current.CancellationToken);
            var result2 = await client.SendAsync("09120000001", "world", null, TestContext.Current.CancellationToken);

            result1.MessageId.Should().Be("mock-1");
            result2.MessageId.Should().Be("mock-2");
            client.Messages.Should().HaveCount(2);
            client.Messages[0].Recipient.Should().Be("09120000000");
            client.Messages[0].MessageText.Should().Be("hello");
            client.Messages[0].SenderLine.Should().Be("3000");
            client.Messages[0].State.Should().Be(MessageDeliveryState.Delivered);
        }

        [Fact]
        public async Task SendAsync_EmptyRecipient_Throws()
        {
            var client = CreateClient();
            Func<Task> act = async () => await client.SendAsync("", "x", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendBulkAsync_RecordsAllRecipients()
        {
            var client = CreateClient();
            var recipients = new[] { "09120000000", "09120000001", "09120000002" };

            var result = await client.SendBulkAsync(recipients, "bulk text", null, TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("mock-1");
            result.RecipientIds.Should().Equal("mock-1", "mock-2", "mock-3");
            client.Messages.Should().HaveCount(3);
            client.Messages.Should().OnlyContain(m => m.MessageText == "bulk text");
            client.Messages.Should().OnlyContain(m => m.State == MessageDeliveryState.Queued);
        }

        [Fact]
        public async Task SendBulkAsync_EmptyRecipients_Throws()
        {
            var client = CreateClient();
            Func<Task> act = async () => await client.SendBulkAsync(Array.Empty<string>(), "x", null, TestContext.Current.CancellationToken);
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task SendOtpAsync_UsesCode_AndRecordsTemplate()
        {
            var client = CreateClient();

            var result = await client.SendOtpAsync(
                "09120000000",
                new OtpRequest { Code = "12345", TemplateId = "verify" },
                TestContext.Current.CancellationToken);

            result.MessageId.Should().Be("mock-1");
            var msg = client.Messages[0];
            msg.MessageText.Should().Be("12345");
            msg.TemplateId.Should().Be("verify");
            msg.State.Should().Be(MessageDeliveryState.Delivered);
        }

        [Fact]
        public async Task SendOtpAsync_FallsBackToTokenParameter()
        {
            var client = CreateClient();

            var result = await client.SendOtpAsync(
                "09120000000",
                new OtpRequest { Parameters = new Dictionary<string, string> { ["token"] = "9876" } },
                TestContext.Current.CancellationToken);

            client.Messages[0].MessageText.Should().Be("9876");
        }

        [Fact]
        public async Task SendOtpAsync_NoCode_NoToken_UsesDefault()
        {
            var client = CreateClient();

            await client.SendOtpAsync("09120000000", new OtpRequest(), TestContext.Current.CancellationToken);

            client.Messages[0].MessageText.Should().Be("000000");
        }

        [Fact]
        public async Task GetMessageStatusAsync_UnknownId_ReturnsUnknown()
        {
            var client = CreateClient();

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("mock-99", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Unknown);
            result.RawStatus.Should().Be("not-found");
        }

        [Fact]
        public async Task GetMessageStatusAsync_FindsByProviderId()
        {
            var client = CreateClient();
            await client.SendAsync("09120000000", "hello", null, TestContext.Current.CancellationToken);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("mock-1", MessageIdentifierType.ProviderMessageId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Delivered);
            result.Recipient.Should().Be("09120000000");
            result.MessageText.Should().Be("hello");
        }

        [Fact]
        public async Task GetMessageStatusAsync_UnknownTypeDoesNotMatch()
        {
            var client = CreateClient();
            await client.SendAsync("09120000000", "hello", null, TestContext.Current.CancellationToken);

            var result = await client.GetMessageStatusAsync(
                new MessageIdentifier("mock-1", MessageIdentifierType.ClientReferenceId),
                TestContext.Current.CancellationToken);

            result.State.Should().Be(MessageDeliveryState.Unknown);
        }

        [Fact]
        public async Task Clear_ResetsStoreAndCounter()
        {
            var client = CreateClient();
            await client.SendAsync("09120000000", "a", null, TestContext.Current.CancellationToken);

            client.Clear();

            client.Messages.Should().BeEmpty();
            var result = await client.SendAsync("09120000000", "b", null, TestContext.Current.CancellationToken);
            result.MessageId.Should().Be("mock-1");
        }
    }
}