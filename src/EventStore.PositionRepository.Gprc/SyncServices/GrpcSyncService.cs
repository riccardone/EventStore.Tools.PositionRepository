using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;

namespace EventStore.PositionRepository.Gprc.SyncServices;

public sealed class GrpcSyncService : ISyncService
{
    private readonly ILogger<GrpcSyncService> _logger;
    private readonly IPositionRepository _positionRepository;
    private readonly KurrentDBClient _conn;
    private readonly IDictionary<string, Action<JsonNode, JsonNode>> _handlingFunctions;
    private CancellationTokenSource? _subscriptionCts;
    private Task? _subscriptionTask;
    private FromAll CatchUpFrom { get; set; }
    private bool _liveProcessingStarted;
    public event ISyncService.AsyncEventHandler? CatchedUpEvent;
    public bool Started { get; private set; }

    public GrpcSyncService(
        IPositionRepository positionRepository,
        KurrentDBClient conn,
        FromAll catchUpFrom,
        IEnumerable<ISyncEventHandler> handlers,
        ILogger<GrpcSyncService> logger)
    {
        _positionRepository = positionRepository;
        _conn = conn;
        CatchUpFrom = catchUpFrom;
        _logger = logger;
        _handlingFunctions = handlers.ToDictionary(h => h.EventType, h => (Action<JsonNode, JsonNode>)h.Handle);
    }

    public Task StartAsync(CancellationToken token)
    {
        try
        {
            _subscriptionCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _subscriptionTask = Task.Run(() => SubscribeLoop(_subscriptionCts.Token), CancellationToken.None);
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }

    public async Task StopAsync(CancellationToken token)
    {
        if (_subscriptionCts is not null)
        {
            await _subscriptionCts.CancelAsync();
        }

        if (_subscriptionTask is not null)
        {
            try
            {
                await _subscriptionTask.WaitAsync(token);
            }
            catch (OperationCanceledException) when ((_subscriptionCts?.IsCancellationRequested).GetValueOrDefault())
            {
                // Expected during normal shutdown.
            }
        }

        _subscriptionCts?.Dispose();
        _subscriptionCts = null;
        _subscriptionTask = null;
        Started = false;
    }

    private async Task SubscribeLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var subscription = _conn.SubscribeToAll(CatchUpFrom, cancellationToken: token);

                _logger.LogInformation("{SyncServiceName} Subscribed from position: {CatchUpFrom}",
                    nameof(GrpcSyncService), CatchUpFrom);

                await foreach (var message in subscription.Messages.WithCancellation(token))
                {
                    switch (message)
                    {
                        case StreamMessage.CaughtUp:
                            TryMarkAsCaughtUp();
                            break;

                        case StreamMessage.Event(var evnt):
                            await EventAppeared(evnt);
                            break;

                        case StreamMessage.AllStreamCheckpointReached(var p):
                            _positionRepository.Set(p);
                            CatchUpFrom = FromAll.After(p);
                            break;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.GetBaseException().Message);
                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
        }
    }

    private void TryMarkAsCaughtUp()
    {
        if (_liveProcessingStarted)
            return;

        OnCatchedUpEvent();
    }

    private void OnCatchedUpEvent()
    {
        _liveProcessingStarted = true;
        Started = true;
        CatchedUpEvent?.Invoke(this, EventArgs.Empty);
    }

    private Task EventAppeared(ResolvedEvent evt)
    {
        try
        {
            if (evt.Event.EventStreamId.StartsWith('$') ||
                evt.Event.EventType.StartsWith('$') ||
                evt.Event.EventType.StartsWith("Position") ||
                evt.Event.EventType.StartsWith("CloudEvent") ||
                !_handlingFunctions.ContainsKey(evt.Event.EventType))
                return Task.CompletedTask;

            var metadataAsJson = Encoding.UTF8.GetString(evt.Event.Metadata.ToArray());
            if (string.IsNullOrWhiteSpace(metadataAsJson))
                return Task.CompletedTask;

            var data = JsonNode.Parse(Encoding.UTF8.GetString(evt.Event.Data.ToArray()))!;
            var metaData = JsonNode.Parse(metadataAsJson)!;

            ProcessEventAppeared(evt.Event.EventType, data, metaData);

            _positionRepository.Set(evt.OriginalPosition!.Value);

            if (_liveProcessingStarted)
                _logger.LogDebug("Handled {EventType} EventId: '{EventId}'", evt.Event.EventType, evt.Event.EventId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.GetBaseException().Message);
        }

        return Task.CompletedTask;
    }

    private void ProcessEventAppeared(string eventType, JsonNode data, JsonNode metaData)
    {
        _handlingFunctions[eventType](data, metaData);
    }
}