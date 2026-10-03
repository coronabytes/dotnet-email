using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Core.Email.Provider.SendGrid;

internal class SendGridProvider : ICoreEmailProvider
{
    private readonly Options _options = new();
    private readonly SendGridClient _sendGrid;

    public SendGridProvider(IConfiguration configuration, [ServiceKey] string key)
    {
        configuration.Bind($"Email:{key}", _options);
        _sendGrid = new SendGridClient(_options.ApiKey);
    }

    public string Name => "SendGrid";

    public long MaxSize => 30 * 1024 * 1024;

    public async Task<List<CoreEmailStatus>> SendBatchAsync(List<CoreEmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var list = new List<CoreEmailStatus>();

        foreach (var message in messages)
            try
            {
                var m = CreateMessage(message);
                var res = await _sendGrid.SendEmailAsync(m, cancellationToken).ConfigureAwait(false);

                list.Add(new CoreEmailStatus
                {
                    Id = message.Id,
                    IsSuccess = res.IsSuccessStatusCode,
                    Error = await res.Body.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false)
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

    internal static SendGridMessage CreateMessage(CoreEmailMessage message)
    {
        var m = MailHelper.CreateSingleEmail(new EmailAddress(message.From, message.FromName),
            new EmailAddress(message.To.First()), message.Subject,
            message.TextBody, message.HtmlBody);

        if (!string.IsNullOrEmpty(message.ReplyTo))
            m.ReplyTo = new EmailAddress(message.ReplyTo);

        foreach (var to in message.To.Skip(1))
            m.AddTo(new EmailAddress(to));

        foreach (var cc in message.Cc)
            m.AddCc(new EmailAddress(cc));

        foreach (var bcc in message.Bcc)
            m.AddBcc(new EmailAddress(bcc));

        foreach (var attachment in message.Attachments)
            m.AddAttachment(attachment.Name, Convert.ToBase64String(attachment.Content),
                attachment.ContentType);

        return m;
    }

    [Serializable]
    private class Options
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}