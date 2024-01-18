using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PostmarkDotNet;

namespace Core.Email.Provider.Postmark;

internal class PostmarkProvider : ICoreEmailProvider
{
    private readonly Options _options = new();
    private readonly PostmarkClient _postmark;

    public PostmarkProvider(IConfiguration configuration, [ServiceKey] string key)
    {
        configuration.Bind($"Email:{key}", _options);
        _postmark = new PostmarkClient(_options.ServerToken);
    }

    public string Name => "Postmark";

    public async Task SendBatchAsync(List<CoreEmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var res = await _postmark.SendMessagesAsync(messages.Select(x => new PostmarkMessage
        {
            From = x.From,
            To = x.To.First(),
            Cc = x.Cc.FirstOrDefault(), // TODO: only one?
            Bcc = x.Bcc.FirstOrDefault(),
            Subject = x.Subject,
            TextBody = x.TextBody,
            HtmlBody = x.HtmlBody,
            MessageStream = _options.MessageStream
        }));
    }

    [Serializable]
    private class Options
    {
        public string ServerToken { get; set; } = string.Empty;
        public string MessageStream { get; set; } = string.Empty;
    }
}