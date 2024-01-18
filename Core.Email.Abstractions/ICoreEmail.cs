namespace Core.Email.Abstractions;

public interface ICoreEmail
{
    public Task SendAsync(CoreEmailMessage message, CancellationToken cancellationToken = default);
}