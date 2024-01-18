using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Email;

internal class CoreEmailService(IServiceProvider serviceProvider, IConfiguration config) : BackgroundService, ICoreEmail
{
    public ICoreEmailProvider? Provider { get; init; } =
        serviceProvider.GetKeyedService<ICoreEmailProvider>(config["Email:Default"]);

    public ICoreEmailPersistence? Persistence { get; init; }

    public async Task<CoreEmailStatus> SendAsync(CoreEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        if (Provider == null)
            throw new InvalidOperationException("default provider not found");

        if (Persistence != null)
        {
            await Persistence.StoreBatchAsync([message], cancellationToken);
            return new CoreEmailStatus { Id = message.Id, IsSuccess = true }; // TODO: ?
        }

        return (await Provider.SendBatchAsync([message], cancellationToken)).First();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Persistence == null || Provider == null)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(10000, stoppingToken).ContinueWith(_ => { }, CancellationToken.None);

            if (stoppingToken.IsCancellationRequested)
                break;

            // TODO: redis lock

            try
            {
                var messages = await Persistence.GetUnsentAsync(CancellationToken.None);
                await Provider.SendBatchAsync(messages, CancellationToken.None);
            }
            catch (Exception e)
            {
                //
            }
        }
    }
}