# Fediverse Hub

Fediverse Hub is a production-oriented .NET scaffold for a cross-platform Fediverse timeline app. It runs in demo mode without real accounts and keeps UI, domain logic, API adapters, persistence, localization and platform shells separated.

# Caveat
Accounts are created in the format -accountname-.peer

## Architecture

- `FediverseHub.Core`: domain models, source/client interfaces, timeline aggregation, hashtag services, compose validation and MVVM view models.
- `FediverseHub.Infrastructure`: mock Fediverse clients, RSS parser, SQLite repositories, config loader, localization service and encrypted file token store.
- `FediverseHub.Maui`: .NET MAUI shell for Android, iOS and Windows with timeline tabs, swipe source switching, pull-to-refresh, onboarding, compose and settings samples.
- `FediverseHub.Linux`: separate Avalonia desktop shell that reuses Core, Infrastructure and ViewModels.
- `FediverseHub.Tests`: unit tests for timeline aggregation, hashtag mapping, RSS parsing and localization behavior.

## Demo Mode

The app registers mock clients for Mastodon, Pixelfed, PeerTube, Lemmy and RSS. The `All` timeline merges items from all five sources, sorts by publish date descending and can optionally rank items by followed hashtags.

Real network clients can be added by implementing:

- `IFediverseSourceClient`
- `IPostPublisher`
- `IMastodonClient`, `IPixelfedClient`, `IPeerTubeClient`, `ILemmyClient`
- `IRegistrationClient`

## Localization

All main UI labels are loaded from JSON resources in `FediverseHub.Infrastructure/Localization/Resources`.

Supported language codes:

`en`, `de`, `fr`, `es`, `it`, `hi`, `ja`, `zh`, `ru`

The localization service uses the system UI culture on first start and falls back to English when unsupported. Manual language selection is persisted in settings.

## Persistence And Security

SQLite stores cache, settings, RSS feeds and schema tables for timeline items, media attachments, accounts, RSS feeds, followed hashtags, interests and instance config. Tokens are never stored in SQLite. The included fallback token store writes encrypted local token files; platform-native secure storage adapters can replace `ISecureTokenStore`.

## Build And Test

```powershell
dotnet restore FediverseHub.sln
dotnet build FediverseHub.Infrastructure\FediverseHub.Infrastructure.csproj
dotnet build FediverseHub.Linux\FediverseHub.Linux.csproj
dotnet build FediverseHub.Maui\FediverseHub.Maui.csproj -f net10.0-windows10.0.19041.0
dotnet test FediverseHub.Tests\FediverseHub.Tests.csproj
```

Run the Linux/Avalonia desktop shell:

```powershell
dotnet run --project FediverseHub.Linux\FediverseHub.Linux.csproj
```

Build Android:

```powershell
dotnet build FediverseHub.Maui\FediverseHub.Maui.csproj -p:EnableMobileTargets=true -f net10.0-android
```

iOS builds require the normal MAUI Apple toolchain on macOS or a paired Mac:

```powershell
dotnet build FediverseHub.Maui\FediverseHub.Maui.csproj -p:EnableMobileTargets=true -f net10.0-ios
```

## Configuration

`fediverse.instances.json` defines the default instances and RSS feeds. It intentionally contains no secrets. Exported app configuration may include language/theme/feed settings, but must not include tokens.

## Current Limitations

- Fediverse OAuth, registration and posting are implemented as robust adapter skeletons with complete mock behavior.
- Real instance capability checks are represented by `SourceCapabilitySet`; production clients should fill this from each instance API.
- OPML import is prepared as a repository/service extension point but not implemented.
- Platform-native secure stores for Android/iOS/Windows/Linux can replace the encrypted fallback behind `ISecureTokenStore`.
