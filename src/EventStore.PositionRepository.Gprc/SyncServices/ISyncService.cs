using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventStore.PositionRepository.Gprc.SyncServices;

public interface ISyncService
{
    event AsyncEventHandler? CatchedUpEvent;
    public delegate Task AsyncEventHandler(object sender, EventArgs e);
    bool Started { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}