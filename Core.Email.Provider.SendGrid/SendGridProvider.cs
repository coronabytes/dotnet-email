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

    public async Task SendBatchAsync(List<CoreEmailMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            var m = MailHelper.CreateSingleEmail(new EmailAddress(""), new EmailAddress(""), message.Subject,
                message.TextBody, message.HtmlBody);

            foreach (var cc in message.Cc)
                m.AddCc(new EmailAddress(cc));

            foreach (var bcc in message.Bcc)
                m.AddCc(new EmailAddress(bcc));

            await _sendGrid.SendEmailAsync(m, cancellationToken);
        }
    }

    [Serializable]
    private class Options
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}