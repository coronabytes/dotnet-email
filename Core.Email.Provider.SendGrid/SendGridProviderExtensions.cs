using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.SendGrid;

public static class SendGridProviderExtensions
{
    /// <summary>
    /// Registers the SendGrid provider, configured from "Email:{key}".
    /// Without a key it is registered as unkeyed provider and configured from "Email:SendGrid".
    /// </summary>
    public static void AddSendGridProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, SendGridProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider>(sp =>
                new SendGridProvider(sp.GetRequiredService<IConfiguration>(), "SendGrid"));
    }
}