using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email;

internal class CoreEmailService(IServiceProvider serviceProvider, IConfiguration config) : ICoreEmail
{
    private readonly ICoreEmailProvider? _defaultProvider =
        serviceProvider.GetKeyedService<ICoreEmailProvider>(config["Email:Default"]);

    public async Task<CoreEmailStatus> SendAsync(CoreEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(message.ProviderKey);

        return (await provider.SendBatchAsync([message], cancellationToken).ConfigureAwait(false)).First();
    }

    public async Task<IReadOnlyCollection<CoreEmailStatus>> SendAsync(List<CoreEmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        // resolve all providers up front, so a missing one fails before anything is sent
        var batches = messages
            .GroupBy(x => x.ProviderKey)
            .Select(x => (Provider: GetProvider(x.Key), Messages: x.ToList()))
            .ToList();

        var res = new List<CoreEmailStatus>(messages.Count);

        foreach (var (provider, batch) in batches)
            res.AddRange(await provider.SendBatchAsync(batch, cancellationToken).ConfigureAwait(false));

        return res;
    }

    private ICoreEmailProvider GetProvider(string? key)
    {
        var provider = key != null
            ? serviceProvider.GetKeyedService<ICoreEmailProvider>(key)
            : _defaultProvider;

        return provider ?? throw new InvalidOperationException($"provider \"{key ?? "Default"}\" not found");
    }
}