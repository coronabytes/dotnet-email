using Core.Email.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.Postmark;

public static class PostmarkProviderExtensions
{
    /// <summary>
    /// Registers the Postmark provider, configured from "Email:{key}".
    /// Without a key it is registered as unkeyed provider and configured from "Email:Postmark".
    /// </summary>
    public static void AddPostmarkProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, PostmarkProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider>(sp =>
                new PostmarkProvider(sp.GetRequiredService<IConfiguration>(), "Postmark"));
    }
}