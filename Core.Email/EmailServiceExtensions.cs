using Core.Email.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Email;

public static class EmailServiceExtensions
{
    public static void AddCoreEmail(this IServiceCollection collection)
    {
        collection.AddSingleton<ICoreEmail, CoreEmailService>();
    }
}