using FluentAssertions;
using Xunit;

namespace IranSms.Tests;

public class IranSmsExceptionTests
{
    [Fact]
    public void DefaultKind_IsUnknownAndNotTransient()
    {
        var exception = new IranSmsException("boom");

        exception.Kind.Should().Be(SmsErrorKind.Unknown);
        exception.IsTransient.Should().BeFalse();
    }

    [Fact]
    public void ProviderRejected_CarriesProviderMetadata()
    {
        var exception = IranSmsException.ProviderRejected("Kavenegar API error (424).", 424, "Kavenegar", "Send");

        exception.Kind.Should().Be(SmsErrorKind.ProviderRejected);
        exception.ProviderStatusCode.Should().Be(424);
        exception.ProviderName.Should().Be("Kavenegar");
        exception.Operation.Should().Be("Send");
        exception.IsTransient.Should().BeFalse();
    }

    [Fact]
    public void RateLimited_IsTransient()
    {
        var exception = IranSmsException.RateLimited("throttled", "Ghasedak");

        exception.Kind.Should().Be(SmsErrorKind.RateLimited);
        exception.IsTransient.Should().BeTrue();
    }

    [Fact]
    public void MalformedResponse_KeepsInnerExceptionAndBody()
    {
        var inner = new FormatException("bad json");

        var exception = IranSmsException.MalformedResponse(
            "Malformed response.",
            inner,
            "SmsIr",
            "{\"result\":null}",
            "Send");

        exception.Kind.Should().Be(SmsErrorKind.MalformedResponse);
        exception.InnerException.Should().BeSameAs(inner);
        exception.RawResponseBody.Should().Be("{\"result\":null}");
        exception.ProviderName.Should().Be("SmsIr");
        exception.Operation.Should().Be("Send");
    }

    [Fact]
    public void MalformedResponse_WorksWithoutInnerException()
    {
        var exception = IranSmsException.MalformedResponse("empty envelope", providerName: "Melipayamak");

        exception.InnerException.Should().BeNull();
        exception.Kind.Should().Be(SmsErrorKind.MalformedResponse);
    }

    [Theory]
    [InlineData(SmsErrorKind.RateLimited, true)]
    [InlineData(SmsErrorKind.Transport, true)]
    [InlineData(SmsErrorKind.Timeout, true)]
    [InlineData(SmsErrorKind.ProviderRejected, false)]
    [InlineData(SmsErrorKind.Unauthorized, false)]
    [InlineData(SmsErrorKind.InsufficientBalance, false)]
    [InlineData(SmsErrorKind.MalformedResponse, false)]
    [InlineData(SmsErrorKind.Cancelled, false)]
    [InlineData(SmsErrorKind.Unknown, false)]
    public void IsTransient_MatchesTheDocumentedMatrix(SmsErrorKind kind, bool expected)
    {
        kind.IsTransient().Should().Be(expected);
    }
}
