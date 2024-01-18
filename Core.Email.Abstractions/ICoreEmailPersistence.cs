namespace Core.Email.Abstractions;

public interface ICoreEmailPersistence
{
    public Task StoreBatchAsync(List<CoreEmailMessage> messages, CancellationToken cancellationToken = default);

    public Task<List<CoreEmailMessage>> GetUnsentAsync(CancellationToken cancellationToken = default);

    public Task UpdateStatusAsync(IDictionary<Guid, string?> updates, CancellationToken cancellationToken = default);
}