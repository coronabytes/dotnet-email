using Core.Email.Abstractions;
using Core.Email.Provider.SES;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.SES;

public static class SendGridProviderExtensions
{
    public static void AddSimpleEmailServiceProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, SimpleEmailServiceProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider, SimpleEmailServiceProvider>();
    }
}