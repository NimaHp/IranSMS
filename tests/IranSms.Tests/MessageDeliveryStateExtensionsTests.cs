using FluentAssertions;
using Xunit;

namespace IranSms.Tests;

public class MessageDeliveryStateExtensionsTests
{
    [Theory]
    [InlineData(MessageDeliveryState.Delivered, true, true, false, false)]
    [InlineData(MessageDeliveryState.Failed, true, false, true, false)]
    [InlineData(MessageDeliveryState.Cancelled, true, false, true, false)]
    [InlineData(MessageDeliveryState.Blocked, true, false, true, false)]
    [InlineData(MessageDeliveryState.Undelivered, true, false, true, false)]
    [InlineData(MessageDeliveryState.Queued, false, false, false, true)]
    [InlineData(MessageDeliveryState.Scheduled, false, false, false, true)]
    [InlineData(MessageDeliveryState.SentToOperator, false, false, false, true)]
    [InlineData(MessageDeliveryState.Unknown, false, false, false, false)]
    public void Classification_MatchesTheDocumentedMatrix(
        MessageDeliveryState state,
        bool isFinal,
        bool isSuccessful,
        bool isFailure,
        bool isPending)
    {
        state.IsFinal().Should().Be(isFinal);
        state.IsSuccessful().Should().Be(isSuccessful);
        state.IsFailure().Should().Be(isFailure);
        state.IsPending().Should().Be(isPending);
    }
}
