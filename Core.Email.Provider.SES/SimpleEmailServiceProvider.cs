using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using System.Net.Mail;

namespace Core.Email.Provider.SES;

internal class SimpleEmailServiceProvider : ICoreEmailProvider
{
    private readonly Options _options = new();

    private readonly AmazonSimpleEmailServiceClient _ses;

    public SimpleEmailServiceProvider(IConfiguration configuration, [ServiceKey] string key)
    {
        configuration.Bind($"Email:{key}", _options);
        _ses = new AmazonSimpleEmailServiceClient(_options.AccessKey, _options.SecretAccessKey,
            RegionEndpoint.GetBySystemName(_options.Region ?? "eu-central-1"));
    }

    public string Name => "SES";

    public long MaxSize => 40 * 1024 * 1024;

    public async Task<List<CoreEmailStatus>> SendBatchAsync(List<CoreEmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var list = new List<CoreEmailStatus>();

        foreach (var message in messages)
            try
            {
                var m = new MimeMessage();
                m.From.Add(new MailboxAddress("", message.From));

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

                using var stream = new MemoryStream();
                await m.WriteToAsync(stream, cancellationToken);
                stream.Position = 0;

                var res = await _ses
                    .SendRawEmailAsync(new SendRawEmailRequest(new RawMessage(stream)), cancellationToken)
                    .ConfigureAwait(false);

                list.Add(new CoreEmailStatus
                {
                    Id = message.Id,
                    ProviderMessageId = res.MessageId,
                    IsSuccess = (int)res.HttpStatusCode >= 200 && (int)res.HttpStatusCode < 300,
                    Error = string.Empty // TODO: ?
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

        return list;
    }

    [Serializable]
    private class Options
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string? Region { get; set; }
    }
}