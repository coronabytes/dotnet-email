using Core.Email.Abstractions;
using Core.Email.Provider.Mailjet;
using Core.Email.Provider.Postmark;
using Core.Email.Provider.SendGrid;
using Core.Email.Provider.SES;
using Core.Email.Provider.SMTP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Core.Email.Tests;

public class EmailTest
{
    [Fact]
    public async Task Test1()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile("appsettings.json");
        configBuilder.AddJsonFile("appsettings.private.json", true);
        var config = configBuilder.Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IConfiguration>(config);

        serviceCollection.AddCoreEmail();
        serviceCollection.AddSmtpProvider("SMTP");
        serviceCollection.AddPostmarkProvider("Postmark");
        serviceCollection.AddSendGridProvider("SendGrid");
        serviceCollection.AddMailjetProvider("MailJet");
        serviceCollection.AddSimpleEmailServiceProvider("SES");
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var email = serviceProvider.GetRequiredService<ICoreEmail>();

        var from = config["TestSetup:From"];
        var to = config["TestSetup:To"];

        await email.SendAsync(new CoreEmailMessage
        {
            To = [to!],
            From = from!,
            Subject = "Transactional Mail Test 3",
            TextBody = "Transactional Mail Test 3"
        });
    }
}