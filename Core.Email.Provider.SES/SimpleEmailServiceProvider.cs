using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;

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

    public async Task SendBatchAsync(List<CoreEmailMessage> messages, CancellationToken cancellationToken = default)
    {
        foreach (var message in messages)
        {
            var m = new MimeMessage();
            m.From.Add(new MailboxAddress("", message.From));

            foreach (var to in message.To)
                m.To.Add(new MailboxAddress(string.Empty, to));

            foreach (var cc in message.Cc)
                m.Cc.Add(new MailboxAddress(string.Empty, cc));

            foreach (var bcc in message.Bcc)
                m.Bcc.Add(new MailboxAddress(string.Empty, bcc));

            m.Subject = message.Subject;
            m.Body = new BodyBuilder
            {
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody
            }.ToMessageBody();

            using var stream = new MemoryStream();
            await m.WriteToAsync(stream, cancellationToken);
            stream.Position = 0;

            await _ses.SendRawEmailAsync(new SendRawEmailRequest(new RawMessage(stream)), cancellationToken);
        }
    }

    [Serializable]
    private class Options
    {
        public string AccessKey { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string? Region { get; set; }
    }
}