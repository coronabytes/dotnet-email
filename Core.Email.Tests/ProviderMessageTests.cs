using Core.Email.Abstractions;
using Core.Email.Provider.Mailjet;
using Core.Email.Provider.Postmark;
using Core.Email.Provider.SendGrid;
using Core.Email.Provider.SES;
using Core.Email.Provider.SMTP;
using Xunit;

namespace Core.Email.Tests;

/// <summary>
/// Checks how each provider maps a <see cref="CoreEmailMessage"/> - nothing is sent.
/// </summary>
public class ProviderMessageTests
{
    private static readonly CoreEmailMessage Message = new()
    {
        From = "noreply@example.com",
        FromName = "Example · Reports",
        To = ["to1@example.com", "to2@example.com"],
        Cc = ["cc@example.com"],
        Bcc = ["bcc@example.com"],
        Subject = "Subject",
        TextBody = "Text"
    };

    [Fact]
    public void Mailjet()
    {
        var m = MailjetProvider.CreateMessage(Message);

        Assert.Equal("noreply@example.com", m.From.Email);
        Assert.Equal("Example · Reports", m.From.Name);
        Assert.Equal(2, m.To.Count);
        Assert.Equal("bcc@example.com", Assert.Single(m.Bcc).Email);
    }

    [Fact]
    public void Postmark()
    {
        var m = PostmarkProvider.CreateMessage(Message, "outbound");

        Assert.Equal("\"Example · Reports\" <noreply@example.com>", m.From);
        Assert.Equal("to1@example.com,to2@example.com", m.To);
        Assert.Equal("cc@example.com", m.Cc);
        Assert.Equal("bcc@example.com", m.Bcc);
    }

    [Fact]
    public void PostmarkWithoutFromNameOrCopies()
    {
        var m = PostmarkProvider.CreateMessage(new CoreEmailMessage { From = "noreply@example.com", To = ["to@example.com"] }, "outbound");

        Assert.Equal("noreply@example.com", m.From);
        Assert.Null(m.Cc);
        Assert.Null(m.Bcc);
    }

    [Fact]
    public void SendGrid()
    {
        var m = SendGridProvider.CreateMessage(Message);
        var p = Assert.Single(m.Personalizations);

        Assert.Equal("Example · Reports", m.From.Name);
        Assert.Equal(["to1@example.com", "to2@example.com"], p.Tos.Select(x => x.Email));
        Assert.Equal("cc@example.com", Assert.Single(p.Ccs).Email);
        Assert.Equal("bcc@example.com", Assert.Single(p.Bccs).Email);
    }

    [Fact]
    public void SesKeepsBccOutOfMime()
    {
        var m = SimpleEmailServiceProvider.CreateMimeMessage(Message);

        Assert.Equal("Example · Reports", m.From.Mailboxes.Single().Name);
        Assert.Equal(2, m.To.Count);
        Assert.Empty(m.Bcc);
    }

    [Fact]
    public void Smtp()
    {
        var m = SmtpProvider.CreateMimeMessage(Message);

        Assert.Equal("Example · Reports", m.From.Mailboxes.Single().Name);
        Assert.Equal(2, m.To.Count);
        Assert.Single(m.Bcc);
    }
}