namespace Core.Email.Abstractions;

public class CoreEmailAttachment
{
    public string Name { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}