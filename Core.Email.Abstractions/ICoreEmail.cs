namespace Core.Email.Abstractions;

public interface ICoreEmail
{
    public Task<CoreEmailStatus> SendAsync(CoreEmailMessage message, CancellationToken cancellationToken = default);
    public Task<IReadOnlyCollection<CoreEmailStatus>> SendAsync(List<CoreEmailMessage> message, CancellationToken cancellationToken = default);
}