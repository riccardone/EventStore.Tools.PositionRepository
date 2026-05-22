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

## Building Projections with GrpcSyncService

`GrpcSyncService` is a ready-made subscription host included in the gRPC package. It manages the KurrentDB catch-up subscription loop, handles reconnections with backoff, and persists the position automatically. You implement only the business logic for the events you care about.

### How it works

- Subscribes to the `$all` stream from the last saved position
- Skips system events (`$`-prefixed), `Position*`, and `CloudEvent*` event types automatically  
- Dispatches each recognised event type to its registered handler (`data`, `metadata` both as `JsonNode`)
- Saves the position after every handled event; also saves checkpoint positions between events
- Fires `CatchedUpEvent` once the subscription has replayed all historical events and gone live
- Reconnects automatically if the subscription drops

### Pattern 1 — Composition (recommended)

Register one `ISyncEventHandler` per event type and let `GrpcSyncService` dispatch to them. This is the simplest path and works well with dependency injection.

```csharp
// 1. Implement a handler for each event type you care about
public class OrderPlacedHandler : ISyncEventHandler
{
    public string EventType => "OrderPlacedV1";

    public void Handle(JsonNode data, JsonNode metadata)
    {
        var orderId = data["orderId"]?.GetValue<string>();
        // update your read model / projection
    }
}

// 2. Register everything in DI (e.g. in Program.cs or a Startup class)
services.AddSingleton<KurrentDBClient>(_ =>
    new KurrentDBClient(KurrentDBClientSettings.Create("esdb://localhost:2113?tls=false")));

services.AddSingleton<IPositionRepository>(sp =>
    new PositionRepository("orders-projection-position", "PositionStored",
        sp.GetRequiredService<KurrentDBClient>()));

services.AddSingleton<ISyncEventHandler, OrderPlacedHandler>();
services.AddSingleton<ISyncEventHandler, OrderCancelledHandler>();

services.AddSingleton<ISyncService>(sp =>
{
    var posRepo = sp.GetRequiredService<IPositionRepository>();
    var client  = sp.GetRequiredService<KurrentDBClient>();
    var handlers = sp.GetServices<ISyncEventHandler>();
    var logger  = sp.GetRequiredService<ILogger<GrpcSyncService>>();

    var catchUpFrom = posRepo.TryGet(out var saved)
        ? FromAll.After(saved)
        : FromAll.Start;

    return new GrpcSyncService(posRepo, client, catchUpFrom, handlers, logger);
});

// 3. Start/stop via the ISyncService interface
var syncService = sp.GetRequiredService<ISyncService>();
await syncService.StartAsync(cancellationToken);

// React when catch-up is complete (all historical events replayed)
syncService.CatchedUpEvent += (_, _) =>
{
    Console.WriteLine("Projection is live.");
    return Task.CompletedTask;
};
```

### Pattern 2 — Inheritance

Subclass `GrpcSyncService` when you need cross-cutting concerns applied to every event before dispatch — for example tenant filtering, custom logging context, or bridging to a different serialiser.

```csharp
public class MyProjectionService : GrpcSyncService
{
    public MyProjectionService(
        IPositionRepository positionRepository,
        KurrentDBClient conn,
        FromAll catchUpFrom,
        IEnumerable<ISyncEventHandler> handlers,
        ILogger<MyProjectionService> logger)
        : base(positionRepository, conn, catchUpFrom, handlers, logger) { }

    // Override to intercept the raw ResolvedEvent before any dispatch
    protected override Task EventAppeared(ResolvedEvent evt)
    {
        // add context, filter, transform — then call base to dispatch normally
        using var scope = _logger.BeginScope(new { evt.Event.EventType });
        return base.EventAppeared(evt);
    }

    // Override to intercept after parsing but just before the handler is called
    protected override void ProcessEventAppeared(string eventType, JsonNode data, JsonNode metadata)
    {
        // e.g. enrich data, log, or call a different dispatch mechanism
        base.ProcessEventAppeared(eventType, data, metadata);
    }
}
```

### Running as an IHostedService

`GrpcSyncService` implements `ISyncService` which maps directly onto the `IHostedService` lifecycle. Wire it up as a hosted service:

```csharp
public class ProjectionWorker : BackgroundService
{
    private readonly ISyncService _syncService;

    public ProjectionWorker(ISyncService syncService) => _syncService = syncService;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => _syncService.StartAsync(stoppingToken);

    public override Task StopAsync(CancellationToken cancellationToken)
        => _syncService.StopAsync(cancellationToken);
}

// Register in Program.cs
services.AddHostedService<ProjectionWorker>();
```

---

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