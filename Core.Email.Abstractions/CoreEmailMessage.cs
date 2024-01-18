namespace Core.Email.Abstractions;

public class CoreEmailMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<string> To { get; init; } = new();
    public List<string> Cc { get; init; } = new();
    public List<string> Bcc { get; init; } = new();
    public string From { get; init; } = string.Empty;
    public string ReplyTo { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string TextBody { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public List<CoreEmailAttachment> Attachments { get; init; } = new();

    public string? ProviderKey { get; set; }
}