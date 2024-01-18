using Core.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.Mailjet;

public static class MailjetProviderExtensions
{
    public static void AddMailjetProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, MailjetProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider, MailjetProvider>();
    }
}