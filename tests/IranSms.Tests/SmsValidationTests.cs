using FluentAssertions;
using Xunit;

namespace IranSms.Tests;

public class SmsValidationTests
{
    [Fact]
    public void EnsureRecipient_RejectsNull()
    {
        FluentActions.Invoking(() => SmsValidation.EnsureRecipient(null!))
            .Should().Throw<ArgumentNullException>()
            .WithParameterName("recipient");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("----")]
    public void EnsureRecipient_RejectsEmptyAfterNormalization(string recipient)
    {
        FluentActions.Invoking(() => SmsValidation.EnsureRecipient(recipient))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureRecipient_TrimsAndNormalizes()
    {
        SmsValidation.EnsureRecipient("  0912 345 6789  ").Should().Be("09123456789");
    }

    [Fact]
    public void EnsureRecipient_TransliteratesPersianDigits()
    {
        SmsValidation.EnsureRecipient("۰۹۱۲۳۴۵۶۷۸۹").Should().Be("09123456789");
    }

    [Fact]
    public void EnsureRecipient_TransliteratesArabicIndicDigits()
    {
        SmsValidation.EnsureRecipient("٠٩١٢٣٤٥٦٧٨٩").Should().Be("09123456789");
    }

    [Fact]
    public void EnsureRecipient_PreservesCountryPrefix()
    {
        SmsValidation.EnsureRecipient("+98-912-345-6789").Should().Be("+989123456789");
        SmsValidation.EnsureRecipient("00989123456789").Should().Be("00989123456789");
    }

    [Fact]
    public void EnsureRecipient_UsesCallerParameterName()
    {
        FluentActions.Invoking(() => SmsValidation.EnsureRecipient(" ", "receptors"))
            .Should().Throw<ArgumentException>()
            .WithParameterName("receptors");
    }

    [Fact]
    public void NormalizeRecipient_RemovesFormattingCharacters()
    {
        SmsValidation.NormalizeRecipient("(0912) 345-6789").Should().Be("09123456789");
    }

    [Fact]
    public void EnsureMessage_RejectsNullAndWhitespace()
    {
        FluentActions.Invoking(() => SmsValidation.EnsureMessage(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => SmsValidation.EnsureMessage("  ")).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EnsureMessage_ReturnsTextUnchanged()
    {
        SmsValidation.EnsureMessage("  سلام  ").Should().Be("  سلام  ");
    }

    [Fact]
    public void EnsureSenderLine_AllowsNullAndTrims()
    {
        SmsValidation.EnsureSenderLine(null).Should().BeNull();
        SmsValidation.EnsureSenderLine("  3000  ").Should().Be("3000");
    }

    [Fact]
    public void EnsureSenderLine_RejectsWhitespace()
    {
        FluentActions.Invoking(() => SmsValidation.EnsureSenderLine(" "))
            .Should().Throw<ArgumentException>()
            .WithParameterName("senderLine");
    }

    [Fact]
    public void EnsureClientReferenceId_AllowsNullAndTrims()
    {
        SmsValidation.EnsureClientReferenceId(null).Should().BeNull();
        SmsValidation.EnsureClientReferenceId("  order-42  ").Should().Be("order-42");
    }

    [Fact]
    public void EnsureClientReferenceId_RejectsWhitespace()
    {
        FluentActions.Invoking(() => SmsValidation.EnsureClientReferenceId("  "))
            .Should().Throw<ArgumentException>();
    }
}
