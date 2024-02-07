using Core.Email.Abstractions;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.Mailjet;

internal class MailjetProvider : ICoreEmailProvider
{
    private readonly MailjetClient _mailjet;
    private readonly Options _options = new();

    public MailjetProvider(IConfiguration configuration, [ServiceKey] string key)
    {
        configuration.Bind($"Email:{key}", _options);
        _mailjet = new MailjetClient(_options.ApiKey, _options.ApiSecret);
    }

    public string Name => "Mailjet";

    public long MaxSize => 15 * 1024 * 1024;

    public async Task<List<CoreEmailStatus>> SendBatchAsync(List<CoreEmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        var batch = messages.Select(x => new TransactionalEmail
        {
            CustomID = x.Id.ToString("N"),
            To = x.To.Select(y => new SendContact(y)).ToList(),
            From = new SendContact(x.From),
            Cc = x.Cc.Select(y => new SendContact(y)).ToList(),
            Bcc = x.Bcc.Select(y => new SendContact(y)).ToList(),
            ReplyTo = string.IsNullOrEmpty(x.ReplyTo) ? null : new SendContact(x.ReplyTo),
            Subject = x.Subject,
            TextPart = x.TextBody,
            HTMLPart = x.HtmlBody,
            Attachments = x.Attachments
                .Select(y => new Attachment(y.Name, y.ContentType, Convert.ToBase64String(y.Content)))
                .ToList()
        }).ToList();

        var res = await _mailjet.SendTransactionalEmailsAsync(batch).ConfigureAwait(false);

        return res.Messages.Select(x =>
        {
            Guid.TryParse(x.CustomID, out var id);

            var status = new CoreEmailStatus
            {
                Id = id,
                IsSuccess = x.Errors == null || x.Errors.Count == 0,
                Error = x.Errors != null ? string.Join("\n", x.Errors.Select(y => y.ErrorMessage)) : string.Empty
            };

            return status;
        }).ToList();
    }

    [Serializable]
    private class Options
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
    }
}