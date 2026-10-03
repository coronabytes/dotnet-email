using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.SMTP;

public static class SmtpProviderExtensions
{
    /// <summary>
    /// Registers the SMTP provider, configured from "Email:{key}".
    /// Without a key it is registered as unkeyed provider and configured from "Email:SMTP".
    /// </summary>
    public static void AddSmtpProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, SmtpProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider>(sp =>
                new SmtpProvider(sp.GetRequiredService<IConfiguration>(), "SMTP"));
    }
}