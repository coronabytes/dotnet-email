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
                var m = MailHelper.CreateSingleEmail(new EmailAddress(message.From),
                    new EmailAddress(message.To.First()), message.Subject,
                    message.TextBody, message.HtmlBody);

                // TODO: add other Tos
                foreach (var cc in message.Cc)
                    m.AddCc(new EmailAddress(cc));

                foreach (var bcc in message.Bcc)
                    m.AddCc(new EmailAddress(bcc));

                foreach (var attachment in message.Attachments)
                    m.AddAttachment(attachment.Name, Convert.ToBase64String(attachment.Content),
                        attachment.ContentType);

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

    [Serializable]
    private class Options
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}