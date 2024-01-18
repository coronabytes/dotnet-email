using Core.Email.Abstractions;
using Mailjet.Client;
using Mailjet.Client.TransactionalEmails;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.Mailjet;

internal class MailjetProvider : ICoreEmailProvider
{
    private readonly Options _options = new();
    private readonly MailjetClient _mailjet;

    public MailjetProvider(IConfiguration configuration, [ServiceKey] string key)
    {
        configuration.Bind($"Email:{key}", _options);
        _mailjet = new MailjetClient(_options.ApiKey, _options.ApiSecret);
    }

    public string Name => "Mailjet";

    public async Task SendBatchAsync(List<CoreEmailMessage> messages, CancellationToken cancellationToken = default)
    {
        await _mailjet.SendTransactionalEmailsAsync(messages.Select(x => new TransactionalEmail
        {
            To = x.To.Select(y => new SendContact(y)).ToList(),
            Cc = x.Cc.Select(y => new SendContact(y)).ToList(),
            Bcc = x.Bcc.Select(y => new SendContact(y)).ToList(),
            Subject = x.Subject,
            TextPart = x.TextBody,
            HTMLPart = x.HtmlBody
        }));
    }

    private class Options
    {
        public string ApiKey { get; } = string.Empty;
        public string ApiSecret { get; } = string.Empty;
    }
}