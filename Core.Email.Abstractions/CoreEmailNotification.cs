namespace Core.Email.Abstractions;

[Serializable]
public class CoreEmailNotification
{
    public string ProviderMessageId { get; set; } = string.Empty;

    public CoreEmailNotificationType Type { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public List<string> Recipients { get; set; } = new();
}