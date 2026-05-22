# Position Repository for KurrentDB

A lightweight library for persisting and retrieving the last processed position in a KurrentDB (formerly EventStoreDB) stream. Useful when you need to resume a subscription from where you left off after a restart.

The position is saved to a dedicated KurrentDB stream (capped at 1 event). To avoid excessive writes during fast processing, saves are batched on a configurable interval (default: 1 second).

## Packages

| Package | Target | Description |
|---|---|---|
| `EventStore.Tools.PositionRepository.Gprc` | net8.0 | Recommended. Uses `KurrentDB.Client` (gRPC) |
| `EventStore.Tools.PositionRepository` | netstandard2.0 | Legacy. Uses the old TCP `EventStore.Client` |

## Installation

### gRPC package (recommended)
```
PM> Install-Package EventStore.Tools.PositionRepository.Gprc
```
```
dotnet add package EventStore.Tools.PositionRepository.Gprc
```

### Legacy TCP package
```
PM> Install-Package EventStore.Tools.PositionRepository
```
```
dotnet add package EventStore.Tools.PositionRepository
```

## Usage

### Create the repository
```csharp
var client = new KurrentDBClient(KurrentDBClientSettings.Create("esdb://localhost:2113?tls=false"));

var positionRepo = new PositionRepository(
    positionStreamName: "my-subscription-position",
    positionEventType: "PositionStored",
    client: client);
```

### Save the current position
```csharp
positionRepo.Set(resolvedEvent.OriginalPosition.Value);
```

### Resume from the last saved position
```csharp
if (positionRepo.TryGet(out var position))
{
    await client.SubscribeToAllAsync(position, eventAppeared);
}
```

### Optional: disable the timer and save on every event
```csharp
// Pass interval: 0 to save immediately on every Set() call
var positionRepo = new PositionRepository("my-position", "PositionStored", client, interval: 0);
```

## Cutting a Release

Releases are published to NuGet via GitHub Actions and triggered manually.

### Prerequisites
- A `NUGET_API_KEY` secret must be set in the repository under *Settings → Secrets and variables → Actions*

### Publish EventStore.Tools.PositionRepository.Gprc
1. Go to **Actions → Publish EventStore.Tools.PositionRepository.Gprc → Run workflow**
2. Leave the version field **empty** to auto-bump the patch (e.g. `1.4.4` → `1.4.5`), or enter a specific version (e.g. `1.5.0`) for a minor/major bump
3. Click **Run workflow**

The workflow will:
- Update the version in the `.csproj`
- Build and pack the NuGet package
- Push it to [NuGet.org](https://www.nuget.org)
- Commit the version bump and create a `gprc-<version>` tag

### Publish EventStore.Tools.PositionRepository (legacy)
Same steps as above but use **Actions → Publish EventStore.Tools.PositionRepository**.  
A `legacy-<version>` tag is created on completion.