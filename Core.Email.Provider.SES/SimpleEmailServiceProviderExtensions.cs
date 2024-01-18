using Core.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.SES;

public static class SimpleEmailServiceProviderExtensions
{
    public static void AddSimpleEmailServiceProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, SimpleEmailServiceProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider, SimpleEmailServiceProvider>();
    }
}