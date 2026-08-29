using FluentAssertions;
using Xunit;

namespace IranSms.Tests;

public class SmsCapabilitiesTests
{
    [Fact]
    public void Capabilities_ArePowersOfTwo_AndDistinct()
    {
        var values = Enum.GetValues<SmsCapabilities>();
        var seen = new HashSet<long>();
        foreach (SmsCapabilities v in values)
        {
            if (v == SmsCapabilities.None)
            {
                continue;
            }

            var l = (long)v;
            (l & (l - 1)).Should().Be(0, $"capability {v} must be a power of two");
            seen.Add(l).Should().BeTrue($"capability {v} must be unique");
        }
    }

    [Theory]
    [InlineData(SmsCapabilities.Send)]
    [InlineData(SmsCapabilities.BulkSend)]
    [InlineData(SmsCapabilities.HeterogeneousSend)]
    [InlineData(SmsCapabilities.ScheduledSend)]
    [InlineData(SmsCapabilities.OtpSend)]
    [InlineData(SmsCapabilities.DeliveryStatus)]
    [InlineData(SmsCapabilities.MessageHistory)]
    [InlineData(SmsCapabilities.Receive)]
    [InlineData(SmsCapabilities.AccountInfo)]
    [InlineData(SmsCapabilities.LineManagement)]
    [InlineData(SmsCapabilities.TemplateManagement)]
    [InlineData(SmsCapabilities.FlashMessage)]
    [InlineData(SmsCapabilities.VoiceMessage)]
    [InlineData(SmsCapabilities.OtpTemplateInspection)]
    public void Supports_NonBoxingCheck_WorksForSingleFlag(SmsCapabilities flag)
    {
        var client = new FakeClient(flag);
        client.Supports(flag).Should().BeTrue();
        client.Supports(SmsCapabilities.None).Should().BeTrue(); // None always "supported"
    }

    [Fact]
    public void Supports_MultiFlag_WorksWithoutHasFlag()
    {
        var client = new FakeClient(SmsCapabilities.Send | SmsCapabilities.OtpSend);
        client.Supports(SmsCapabilities.Send).Should().BeTrue();
        client.Supports(SmsCapabilities.OtpSend).Should().BeTrue();
        client.Supports(SmsCapabilities.BulkSend).Should().BeFalse();
        client.Supports(SmsCapabilities.DeliveryStatus).Should().BeFalse();
    }

    private sealed class FakeClient : ISmsClient
    {
        public FakeClient(SmsCapabilities caps) => Capabilities = caps;
        public string ProviderName => "Fake";
        public SmsCapabilities Capabilities { get; }

        public System.Threading.Tasks.Task<SmsSendResult> SendAsync(
            string recipient, string message, string? senderLine = null,
            System.Threading.CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.FromResult(new SmsSendResult("1"));
    }
}
