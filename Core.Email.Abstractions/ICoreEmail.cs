namespace Core.Email.Abstractions;

public interface ICoreEmail
{
    public Task<CoreEmailStatus> SendAsync(CoreEmailMessage message, CancellationToken cancellationToken = default);
}