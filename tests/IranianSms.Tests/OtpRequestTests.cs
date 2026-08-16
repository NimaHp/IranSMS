using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace IranianSms.Tests;

public class OtpRequestTests
{
    [Fact]
    public void TemplateBasedRequest_BindsTemplateAndParameters()
    {
        var req = new OtpRequest
        {
            TemplateId = "123456",
            Parameters = new Dictionary<string, string> { ["Code"] = "12345" },
            SenderLine = "3000",
        };

        req.TemplateId.Should().Be("123456");
        req.Parameters!["Code"].Should().Be("12345");
        req.SenderLine.Should().Be("3000");
        req.Code.Should().BeNull();
        req.SendDate.Should().BeNull();
    }

    [Fact]
    public void CodeBasedRequest_BindsCode()
    {
        var req = new OtpRequest { Code = "98765" };
        req.Code.Should().Be("98765");
        req.TemplateId.Should().BeNull();
    }

    [Fact]
    public void SupportsScheduledSendDate()
    {
        var when = DateTimeOffset.UtcNow.AddHours(2);
        var req = new OtpRequest { Code = "1", SendDate = when };
        req.SendDate.Should().Be(when);
    }
}