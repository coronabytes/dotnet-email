[![Nuget](https://img.shields.io/nuget/v/Core.Email)](https://www.nuget.org/packages/Core.Email)
[![Nuget](https://img.shields.io/nuget/dt/Core.Email)](https://www.nuget.org/packages/Core.Email)
[![Build](https://github.com/coronabytes/dotnet-email/actions/workflows/build.yml/badge.svg)](https://github.com/coronabytes/dotnet-email/actions/workflows/build.yml)

# .NET Transactional E-Mail Abstraction Layer

Send transactional e-mail through one `ICoreEmail` interface and switch providers by configuration.
Several providers - or several configurations of the same provider - can be registered side by side as keyed services.

Targets .NET 10.

## Packages

| Package | Sends via |
|---|---|
| [Core.Email](https://www.nuget.org/packages/Core.Email) | core service (`ICoreEmail`) - always needed |
| [Core.Email.Abstractions](https://www.nuget.org/packages/Core.Email.Abstractions) | message, status and provider contracts |
| [Core.Email.Provider.SMTP](https://www.nuget.org/packages/Core.Email.Provider.SMTP) | any SMTP server (MailKit) |
| [Core.Email.Provider.SES](https://www.nuget.org/packages/Core.Email.Provider.SES) | Amazon SES, v2 API |
| [Core.Email.Provider.Mailjet](https://www.nuget.org/packages/Core.Email.Provider.Mailjet) | Mailjet |
| [Core.Email.Provider.SendGrid](https://www.nuget.org/packages/Core.Email.Provider.SendGrid) | SendGrid |
| [Core.Email.Provider.Postmark](https://www.nuget.org/packages/Core.Email.Provider.Postmark) | Postmark |

```
dotnet add package Core.Email
dotnet add package Core.Email.Provider.SES
```

## Configuration

appsettings.json - only the providers you register need a section:
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

- `Email:Default` is the key of the provider used for messages without a `ProviderKey`.
- Each provider reads its settings from `Email:{key}`.

## Registration

```csharp
services.AddCoreEmail();
services.AddSmtpProvider("SMTP");
services.AddPostmarkProvider("Postmark");
services.AddSendGridProvider("SendGrid");
services.AddMailjetProvider("Mailjet");
services.AddSimpleEmailServiceProvider("SES");
```

The key is free to choose, so one provider can be registered several times:
```csharp
services.AddPostmarkProvider("Postmark");          // reads Email:Postmark
services.AddPostmarkProvider("PostmarkBroadcast"); // reads Email:PostmarkBroadcast
```

Without a key (`services.AddSmtpProvider()`) the provider is registered unkeyed, reads `Email:SMTP` (or `Email:SES`, `Email:Postmark`, ...) and is used as default while `Email:Default` is not set.

## Sending

```csharp
var email = serviceProvider.GetRequiredService<ICoreEmail>();

var status = await email.SendAsync(new CoreEmailMessage
{
    From = "noreply@example.com",
    FromName = "Example Inc. · Reports", // optional display name
    To = ["jane@example.com"],
    Bcc = ["archive@example.com"],
    ReplyTo = "support@example.com",
    Subject = "Weekly report",
    TextBody = "Your weekly report is attached.",
    HtmlBody = "<p>Your weekly report is attached.</p>",
    Attachments =
    [
        new CoreEmailAttachment
        {
            Name = "report.csv",
            ContentType = "text/csv",
            Content = csvBytes
        }
    ]
});

if (!status.IsSuccess)
    logger.LogWarning("Mail {Id} failed: {Error}", status.Id, status.Error);
```

- **Choose a provider per message** with `ProviderKey = "SES"`; without it the default provider is used.
- **Batches:** `SendAsync(List<CoreEmailMessage>)` groups the messages by `ProviderKey` and hands each group to its provider as one batch. It returns one `CoreEmailStatus` per message - match them by `Id`.
- **Errors:** an unknown `ProviderKey` throws `InvalidOperationException` before anything is sent. Rejected messages come back with `IsSuccess = false` and `Error`. SMTP, SES and SendGrid also report connection and API exceptions that way; Mailjet and Postmark send the whole batch in one API call, which can throw.
- `ProviderMessageId` holds the provider's own message id where available (SES, Postmark).

## Provider notes

| Provider | Size limit¹ | Notes |
|---|---|---|
| SMTP | set by the server | one connection per batch; login is skipped when `Username` is empty; `Tls: true` uses STARTTLS |
| SES | 40 MB | sent as raw MIME through the SES v2 API; `Region` defaults to `eu-central-1` |
| Mailjet | 15 MB | whole batch in one API call |
| SendGrid | 30 MB | one API call per message; To and Cc recipients see each other |
| Postmark | 10 MB | whole batch in one API call; at most 50 recipients (To + Cc + Bcc) per message; `MessageStream` selects the stream |

¹ exposed as `ICoreEmailProvider.MaxSize` for information - not enforced by the library.

## Development

```
dotnet build
dotnet test --filter "Category!=Integration"
```

`EmailTest` is an integration test that sends a real e-mail. It needs provider credentials plus `TestSetup:From` / `TestSetup:To` in `Core.Email.Tests/appsettings.private.json` (git-ignored).

## License

Apache-2.0
