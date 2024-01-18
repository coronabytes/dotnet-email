namespace Core.Email.Abstractions;

public interface ICoreEmailProvider
{
    public string Name { get; }
    public Task SendBatchAsync(List<CoreEmailMessage> messages, CancellationToken cancellationToken = default);
}