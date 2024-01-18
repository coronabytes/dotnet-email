using Core.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.SendGrid;

public static class SendGridProviderExtensions
{
    public static void AddSendGridProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, SendGridProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider, SendGridProvider>();
    }
}