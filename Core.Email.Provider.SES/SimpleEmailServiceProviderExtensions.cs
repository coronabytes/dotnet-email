using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.SES;

public static class SimpleEmailServiceProviderExtensions
{
    /// <summary>
    /// Registers the SES provider, configured from "Email:{key}".
    /// Without a key it is registered as unkeyed provider and configured from "Email:SES".
    /// </summary>
    public static void AddSimpleEmailServiceProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, SimpleEmailServiceProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider>(sp =>
                new SimpleEmailServiceProvider(sp.GetRequiredService<IConfiguration>(), "SES"));
    }
}