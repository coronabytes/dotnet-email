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
    public long MaxSize => 0;

    public async Task<List<CoreEmailStatus>> SendBatchAsync(List<CoreEmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient();

        await client.ConnectAsync(_options.Host, _options.Port,
                _options.Tls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, cancellationToken)
            .ConfigureAwait(false);
        await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken).ConfigureAwait(false);

        var list = new List<CoreEmailStatus>();

        foreach (var message in messages)
            try
            {
                var m = new MimeMessage();
                m.From.Add(new MailboxAddress(string.Empty, message.From));

                if (!string.IsNullOrEmpty(message.ReplyTo))
                    m.ReplyTo.Add(new MailboxAddress(string.Empty, message.ReplyTo));

                foreach (var to in message.To)
                    m.To.Add(new MailboxAddress(string.Empty, to));

                foreach (var cc in message.Cc)
                    m.Cc.Add(new MailboxAddress(string.Empty, cc));

                foreach (var bcc in message.Bcc)
                    m.Bcc.Add(new MailboxAddress(string.Empty, bcc));

                m.Subject = message.Subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = message.HtmlBody,
                    TextBody = message.TextBody
                };

                foreach (var attachment in message.Attachments)
                    bodyBuilder.Attachments.Add(attachment.Name, attachment.Content,
                        ContentType.Parse(attachment.ContentType));

                m.Body = bodyBuilder.ToMessageBody();

                var res = await client.SendAsync(m, cancellationToken).ConfigureAwait(false);

                list.Add(new CoreEmailStatus
                {
                    Id = message.Id,
                    IsSuccess = true, // TODO: ?
                    Error = res
                });
            }
            catch (Exception e)
            {
                list.Add(new CoreEmailStatus
                {
                    Id = message.Id,
                    IsSuccess = false,
                    Error = e.Message
                });
            }

        await client.DisconnectAsync(true, cancellationToken).ConfigureAwait(false);

        return list;
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