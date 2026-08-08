using System;
using FluentAssertions;
using Xunit;

namespace IranianSms.Tests;

public class MessageIdentifierTests
{
    [Fact]
    public void Constructor_StoresValueAndType()
    {
        var id = new MessageIdentifier("abc-123", MessageIdentifierType.ClientReferenceId);
        id.Value.Should().Be("abc-123");
        id.Type.Should().Be(MessageIdentifierType.ClientReferenceId);
        id.ToString().Should().Be("ClientReferenceId:abc-123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsNullOrWhitespace(string? value)
    {
        FluentActions.Invoking(() => new MessageIdentifier(value!, MessageIdentifierType.ProviderMessageId))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_IsByValueAndType()
    {
        var a = new MessageIdentifier("42", MessageIdentifierType.ProviderMessageId);
        var b = new MessageIdentifier("42", MessageIdentifierType.ProviderMessageId);
        var c = new MessageIdentifier("42", MessageIdentifierType.ClientReferenceId);
        var d = new MessageIdentifier("43", MessageIdentifierType.ProviderMessageId);

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        (a != d).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equal_IsOrdinalCaseSensitive()
    {
        var a = new MessageIdentifier("ABC", MessageIdentifierType.ProviderMessageId);
        var b = new MessageIdentifier("abc", MessageIdentifierType.ProviderMessageId);
        a.Equals(b).Should().BeFalse();
    }
}