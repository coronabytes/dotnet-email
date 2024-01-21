using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Email;

internal class CoreEmailService(IServiceProvider serviceProvider, IConfiguration config) : BackgroundService, ICoreEmail
{
    private readonly ICoreEmailProvider? _defaultProvider =
        serviceProvider.GetKeyedService<ICoreEmailProvider>(config["Email:Default"]);

    private readonly ICoreEmailPersistence? _persistence = serviceProvider.GetService<ICoreEmailPersistence>();

    public async Task<CoreEmailStatus> SendAsync(CoreEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var provider = message.ProviderKey != null
            ? serviceProvider.GetKeyedService<ICoreEmailProvider>(message.ProviderKey)
            : _defaultProvider;

        if (provider == null)
            throw new InvalidOperationException($"provider \"{message.ProviderKey ?? "Default"}\" not found");

        if (_persistence != null)
        {
            await _persistence.StoreBatchAsync([message], cancellationToken);
            return new CoreEmailStatus { Id = message.Id, IsSuccess = true };
        }

        return (await provider.SendBatchAsync([message], cancellationToken)).First();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_persistence == null)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken).ContinueWith(_ => { }, CancellationToken.None);

            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                var messages = await _persistence.GetUnsentAsync(CancellationToken.None);
                foreach (var grouping in messages.GroupBy(x => x.ProviderKey))
                {
                    var key = grouping.Key;
                    var provider = key != null
                        ? serviceProvider.GetKeyedService<ICoreEmailProvider>(key)
                        : _defaultProvider;

                    if (provider == null)
                        continue;

                    var res = await provider.SendBatchAsync(messages, CancellationToken.None);
                    var updates = res.ToDictionary(x => x.Id, x => x.IsSuccess ? null : x.Error);
                    await _persistence.UpdateStatusAsync(updates, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                // TODO: 
            }
        }
    }
}