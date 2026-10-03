# AGENTS.md

Transactional e-mail abstraction for .NET 10. `ICoreEmail` (Core.Email) hands `CoreEmailMessage`s to keyed `ICoreEmailProvider`s; every provider ships as its own NuGet package.

## Layout

- `Core.Email.Abstractions` - contracts used by every package: `CoreEmailMessage`, `CoreEmailAttachment`, `CoreEmailStatus`, `ICoreEmail`, `ICoreEmailProvider`. Changes here reach all consumers - keep them additive (new optional members, no renames/removals).
- `Core.Email` - `CoreEmailService` (resolves the provider from `ProviderKey`, falling back to `Email:Default`) and `AddCoreEmail()`.
- `Core.Email.Provider.{SMTP,SES,Mailjet,SendGrid,Postmark}` - one provider each.
- `Core.Email.Tests` - xunit.

## Commands

```
dotnet build
dotnet test --filter "Category!=Integration"
```

`EmailTest` (`Category=Integration`) sends a real e-mail using credentials from `Core.Email.Tests/appsettings.private.json` (git-ignored). Don't run it unless asked, and never commit credentials.

## Provider pattern

- `internal class XProvider : ICoreEmailProvider` with constructor `(IConfiguration configuration, [ServiceKey] string key)`, binding a private `Options` class from `Email:{key}`.
- Message mapping lives in `internal static CreateMessage(...)` / `CreateMimeMessage(...)` and is covered by `ProviderMessageTests` (via `InternalsVisibleTo`). Keep `SendBatchAsync` limited to I/O and status mapping.
- `XProviderExtensions.AddXProvider(string? key = null)`: keyed singleton; without a key an unkeyed singleton configured from `Email:{ProviderName}`.
- `SendBatchAsync` returns exactly one `CoreEmailStatus` per input message, carrying the message's `Id`. Per-message failures become `IsSuccess = false` + `Error` instead of exceptions (Mailjet and Postmark send the batch in one API call, which may throw).
- Every provider must honour every `CoreEmailMessage` field: all To/Cc/Bcc recipients, `FromName`, `ReplyTo`, attachments. Bcc recipients must never appear in visible headers.
- Adding a provider: new project shaped like the existing ones, add it to `Core.Email.sln`, a mapping test in `ProviderMessageTests`, package versions in `Directory.Packages.props`, and rows in both README tables (Packages, Provider notes).

## Conventions

- Shared settings (target framework, NuGet metadata, README packing) live in `Directory.Build.props`; project files only carry `Description`, extra `PackageTags` and references.
- Central package management: versions only in `Directory.Packages.props`, `PackageReference` without `Version`.
- C#: file-scoped namespaces, nullable enabled, `ConfigureAwait(false)` on awaits in library code. The build must stay warning-free.
- Keep each file's existing encoding and line endings - most `.cs`/`.csproj` files are UTF-8 with BOM and CRLF.
- `README.md` is packed into every NuGet package and shown on nuget.org: keep it user-facing and update it when public API or provider behaviour changes.

## Release

- The package version comes from the git tag: pushing `X.Y.Z` runs `.github/workflows/release.yml` (test, pack with `-p:Version`, push to nuget.org). `<Version>` in `Directory.Build.props` stays `1.0.0` and only applies to local builds.
- Never create or push tags, or publish packages, unless the maintainer asks.
