using Core.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.SMTP;

public static class SmtpProviderExtensions
{
    public static void AddSmtpProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, SmtpProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider, SmtpProvider>();
    }
}