namespace Core.Email.Abstractions;

public class CoreEmailMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<string> To { get; init; } = new();
    public List<string> Cc { get; init; } = new();
    public List<string> Bcc { get; init; } = new();
    public string From { get; init; } = string.Empty;

    /// <summary>
    /// Optional display name shown next to <see cref="From"/> in the recipient's mail client
    /// (e.g. "Contoso · Weekly report"). The sending address itself is not affected.
    /// </summary>
    public string? FromName { get; init; }

    public string ReplyTo { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string TextBody { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public List<CoreEmailAttachment> Attachments { get; init; } = new();

    public string? ProviderKey { get; set; }
}