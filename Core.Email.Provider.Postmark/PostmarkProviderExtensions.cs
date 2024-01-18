using Core.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email.Provider.Postmark;

public static class PostmarkProviderExtensions
{
    public static void AddPostmarkProvider(this IServiceCollection collection, string? key = null)
    {
        if (key != null)
            collection.AddKeyedSingleton<ICoreEmailProvider, PostmarkProvider>(key);
        else
            collection.AddSingleton<ICoreEmailProvider, PostmarkProvider>();
    }
}