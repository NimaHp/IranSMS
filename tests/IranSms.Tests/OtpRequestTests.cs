using FluentAssertions;
using Xunit;

namespace IranSms.Tests;

public class OtpRequestTests
{
    [Fact]
    public void TemplateRequest_BindsTemplateAndParameters()
    {
        var req = new OtpTemplateRequest("123456")
        {
            Parameters = new Dictionary<string, string> { ["token"] = "12345" },
            SenderLine = "3000",
        };

        req.TemplateId.Should().Be("123456");
        req.Parameters!["token"].Should().Be("12345");
        req.SenderLine.Should().Be("3000");
        req.SendDate.Should().BeNull();
    }

    [Fact]
    public void CodeRequest_BindsCode()
    {
        var req = new OtpCodeRequest("98765");
        req.Code.Should().Be("98765");
    }

    [Fact]
    public void Both_Requests_ShareTheBaseType()
    {
        OtpRequest template = new OtpTemplateRequest("tpl");
        OtpRequest code = new OtpCodeRequest("1");

        template.Should().BeOfType<OtpTemplateRequest>();
        code.Should().BeOfType<OtpCodeRequest>();
        code.Should().NotBeOfType<OtpTemplateRequest>();
    }

    [Fact]
    public void SupportsScheduledSendDate()
    {
        var when = DateTimeOffset.UtcNow.AddHours(2);
        var req = new OtpCodeRequest("1") { SendDate = when };
        req.SendDate.Should().Be(when);
    }

    [Fact]
    public void SupportsClientReferenceId()
    {
        var req = new OtpCodeRequest("1") { ClientReferenceId = "order-42" };
        req.ClientReferenceId.Should().Be("order-42");
    }

    [Fact]
    public void ClientReferenceId_IsOptional()
    {
        var req = new OtpTemplateRequest("tpl");
        req.ClientReferenceId.Should().BeNull();
    }

    [Fact]
    public void SetParameter_AddsAndOverwrites()
    {
        var req = new OtpTemplateRequest("tpl")
            .SetParameter("param1", "11111")
            .SetParameter("param2", "22222")
            .SetParameter("param1", "33333");

        req.Parameters.Should().HaveCount(2);
        req.Parameters!["param1"].Should().Be("33333");
        req.Parameters["param2"].Should().Be("22222");
    }

    [Fact]
    public void SetParameter_IsCaseInsensitive()
    {
        var req = new OtpTemplateRequest("tpl").SetParameter("TOKEN", "12345");
        req.Parameters!.Should().ContainKey("token");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankValues(string? value)
    {
        FluentActions.Invoking(() => new OtpTemplateRequest(value!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("templateId");
        FluentActions.Invoking(() => new OtpCodeRequest(value!))
            .Should().Throw<ArgumentException>()
            .WithParameterName("code");
    }

    [Theory]
    [InlineData(null, "v")]
    [InlineData("", "v")]
    [InlineData("  ", "v")]
    [InlineData("n", null)]
    [InlineData("n", "")]
    [InlineData("n", "  ")]
    public void SetParameter_RejectsBlankInput(string? name, string? value)
    {
        FluentActions.Invoking(() => new OtpTemplateRequest("tpl").SetParameter(name!, value!))
            .Should().Throw<ArgumentException>();
    }
}
