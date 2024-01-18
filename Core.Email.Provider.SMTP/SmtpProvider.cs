using Core.Email.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

namespace Core.Email.Provider.SMTP;

internal class SmtpProvider : ICoreEmailProvider
{
    private readonly Options _options = new();

    public SmtpProvider(IConfiguration configuration, [ServiceKey] string key)
    {
        configuration.Bind($"Email:{key}", _options);
    }

    public string Name => "SMTP";

    public async Task SendBatchAsync(List<CoreEmailMessage> messages, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(_options.Host, _options.Port,
            _options.Tls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken);
        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

        foreach (var message in messages)
        {
            var m = new MimeMessage();
            m.From.Add(new MailboxAddress("", message.From));
            m.To.Add(new MailboxAddress("", message.To.FirstOrDefault()));
            m.Subject = message.Subject;
            m.Body = new TextPart("plain") { Text = message.TextBody };

            await client.SendAsync(m, cancellationToken);
        }

        await client.DisconnectAsync(true, cancellationToken);
    }

    [Serializable]
    private class Options
    {
        public string Host { get; set; } = string.Empty;
        public short Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool Tls { get; set; }
    }
}