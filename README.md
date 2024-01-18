[![Nuget](https://img.shields.io/nuget/v/Core.Email)](https://www.nuget.org/packages/Core.Email)
[![Nuget](https://img.shields.io/nuget/dt/Core.Email)](https://www.nuget.org/packages/Core.Email)

```
dotnet add package Core.Email
dotnet add package Core.Email.Provider.SES
```

# .NET Transactional E-Mail Abstraction Layer
- open source (Apache 2.0)
- common providers
  - smtp via mailkit
  - aws ses
  - mailjet
  - sendgrid
  - postmark
- TODO
  - attachments
  - bounce handlers  
 
# Usage
appsettings.json
```json
{
  "Email": {
    "Default": "Postmark",
    "SMTP": {
      "Host": "smtp.***.com",
      "Port": 587,
      "Username": "***",
      "Password": "***",
      "Tls": true
    },
    "SES": {
      "AccessKey": "***",
      "SecretAccessKey": "***",
      "Region": "eu-central-1"
    },
    "Postmark": {
      "ServerToken": "***",
      "MessageStream": "outbound"
    },
    "Mailjet": {
      "ApiKey": "***",
      "ApiSecret": "***"
    },
    "SendGrid": {
      "ApiKey": "***"
    }
  }
}
```

- use .NET 8 keyed services to have multiple configurations of the same provider
- appsettings is matched with "Email:{key}"
```csharp
serviceCollection.AddCoreEmail();
serviceCollection.AddSmtpProvider("SMTP");
serviceCollection.AddPostmarkProvider("Postmark");
serviceCollection.AddSendGridProvider("SendGrid");
serviceCollection.AddMailjetProvider("Mailjet");
serviceCollection.AddSimpleEmailServiceProvider("SES");

var email = serviceProvider.GetRequiredService<ICoreEmail>();

await email.SendAsync(new CoreEmailMessage
{
  To = ["test@example.com"],
  From = "test@example.com",
  Subject = "Transactional Mail Subject",
  TextBody = "Transactional Mail Body"
});
```
