using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.Mailjet;

public static class MailjetProviderExtensions
{
    /// <summary>
    /// Registers the Mailjet provider, configured from "Email:{key}".
    /// Without a key it is registered as unkeyed provider and configured from "Email:Mailjet".
    /// </summary>
    public static void AddMailjetProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, MailjetProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider>(sp =>
                new MailjetProvider(sp.GetRequiredService<IConfiguration>(), "Mailjet"));
    }
}