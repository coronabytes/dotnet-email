namespace Core.Email.Abstractions;

public interface ICoreEmailProvider
{
    public string Name { get; }
    public long MaxSize { get; }

    public Task<List<CoreEmailStatus>> SendBatchAsync(List<CoreEmailMessage> messages,
        CancellationToken cancellationToken = default);
}