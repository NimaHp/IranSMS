using FluentAssertions;
using Xunit;

namespace IranSms.Tests;

public class SmsBulkSendResultTests
{
    [Fact]
    public void Constructor_RejectsNullAndEmptyItems()
    {
        FluentActions.Invoking(() => new SmsBulkSendResult(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new SmsBulkSendResult(Array.Empty<SmsSendItemResult>()))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Item_Success_HasNoError()
    {
        var item = SmsSendItemResult.Success("09120000001", "987");

        item.Succeeded.Should().BeTrue();
        item.IsTransient.Should().BeFalse();
        item.MessageId.Should().Be("987");
        item.ErrorKind.Should().BeNull();
        item.ErrorMessage.Should().BeNull();
        item.ProviderStatusCode.Should().BeNull();
    }

    [Fact]
    public void Item_Failure_CarriesClassification()
    {
        var item = SmsSendItemResult.Failure("09120000002", "rate limited", SmsErrorKind.RateLimited, 429);

        item.Succeeded.Should().BeFalse();
        item.IsTransient.Should().BeTrue();
        item.MessageId.Should().BeNull();
        item.ErrorKind.Should().Be(SmsErrorKind.RateLimited);
        item.ProviderStatusCode.Should().Be(429);
    }

    [Fact]
    public void Item_RejectsEmptyRecipientAndNullMessage()
    {
        FluentActions.Invoking(() => SmsSendItemResult.Success(" ")).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => SmsSendItemResult.Failure("09120000001", null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AllSucceeded_CountsEveryRecipient()
    {
        var result = new SmsBulkSendResult(new[]
        {
            SmsSendItemResult.Success("09120000001", "1"),
            SmsSendItemResult.Success("09120000002", "2"),
        })
        {
            TotalCost = 1200m,
        };

        result.Items.Should().HaveCount(2);
        result.SucceededCount.Should().Be(2);
        result.FailedCount.Should().Be(0);
        result.AllSucceeded.Should().BeTrue();
        result.IsPartialFailure.Should().BeFalse();
        result.AllFailed.Should().BeFalse();
        result.TotalCost.Should().Be(1200m);
    }

    [Fact]
    public void IsPartialFailure_WhenSomeRecipientsRejected()
    {
        var result = new SmsBulkSendResult(new[]
        {
            SmsSendItemResult.Success("09120000001", "1"),
            SmsSendItemResult.Failure("09120000002", "invalid number", SmsErrorKind.ProviderRejected, 424),
            SmsSendItemResult.Success("09120000003", "3"),
        });

        result.SucceededCount.Should().Be(2);
        result.FailedCount.Should().Be(1);
        result.AllSucceeded.Should().BeFalse();
        result.IsPartialFailure.Should().BeTrue();
        result.AllFailed.Should().BeFalse();
    }

    [Fact]
    public void AllFailed_WhenEveryRecipientRejected()
    {
        var result = new SmsBulkSendResult(new[]
        {
            SmsSendItemResult.Failure("09120000001", "timeout", SmsErrorKind.Timeout),
            SmsSendItemResult.Failure("09120000002", "timeout", SmsErrorKind.Timeout),
        });

        result.SucceededCount.Should().Be(0);
        result.FailedCount.Should().Be(2);
        result.AllFailed.Should().BeTrue();
        result.IsPartialFailure.Should().BeFalse();
    }

    [Fact]
    public void Constructor_RejectsNullEntries()
    {
        FluentActions.Invoking(() => new SmsBulkSendResult(new SmsSendItemResult[] { null! }))
            .Should().Throw<ArgumentException>();
    }
}
