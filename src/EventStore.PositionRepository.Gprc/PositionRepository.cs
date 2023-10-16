using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Timers;
using EventStore.Client;

namespace EventStore.PositionRepository.Gprc;

public class PositionRepository : IPositionRepository
{
    private readonly ILogger _log;
    private readonly string _positionStreamName;
    private readonly int _interval;
    public string PositionEventType { get; }
    private readonly EventStoreClient _connection;
    private static Timer _timer;
    private Position _position = Position.Start;
    private Position _lastSavedPosition = Position.Start;

    public PositionRepository(string positionStreamName, string positionEventType, EventStoreClient client,
        ILogger logger, int interval = 1000)
    {
        _positionStreamName = positionStreamName;
        _connection = client;
        _interval = interval;
        PositionEventType = positionEventType;
        if (interval <= 0) return;
        _timer = new Timer(interval);
        _timer.Elapsed += _timer_Elapsed;
        _timer.Enabled = true;
        _log = logger;
        _timer.Start();
        InitStream();
    }

    public PositionRepository(string positionStreamName, string positionEventType, EventStoreClient client,
        int interval = 1000) : this(positionStreamName, positionEventType, client,
        new SimpleConsoleLogger(nameof(PositionRepository)), interval)
    {
    }

    private void _timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        if (_lastSavedPosition.Equals(_position))
            return;
        SavePosition();
    }

    private void InitStream()
    {
        try
        {
            _connection?.SetStreamMetadataAsync(_positionStreamName, StreamState.Any,
                SerializeMetadata(new Dictionary<string, int> { { "$maxCount", 1 } })).Wait();
        }
        catch (Exception ex)
        {
            _log.Error("Error while initializing stream", ex);
        }
    }

    private void SavePosition()
    {
        _connection.AppendToStreamAsync(_positionStreamName, StreamState.Any,
            new[] { new EventData(Uuid.FromGuid(Guid.NewGuid()), PositionEventType,
                SerializeObject(_position), null) }).Wait(); //Not sure what to do about the null metadata
        _lastSavedPosition = _position;
    }

    public Position Get()
    {
        try
        {
            var evts = _connection.ReadStreamAsync(Direction.Backwards, _positionStreamName, StreamPosition.End, 20, false).ToArrayAsync().Result;
            if (evts.Length > 0 && evts.First().OriginalPosition != null)
                _position = (Position)evts.First().OriginalPosition;
            else _position = Position.Start;
        }
        catch (Exception e)
        {
            _log.Error($"Error while reading the position: {e.GetBaseException().Message}");
        }
        return _position;
    }

    public void Set(Position position)
    {
        _position = position;
        if (_interval <= 0)
            SavePosition();
    }

    private static StreamMetadata SerializeMetadata(object obj)
    {
        var jsonObj = JsonSerializer.Serialize(obj);
        var data = Encoding.UTF8.GetBytes(jsonObj);
        return new StreamMetadata(customMetadata: System.Text.Json.JsonDocument.Parse(data));
    }

    private static ReadOnlyMemory<byte> SerializeObject(Position position)
    {
        var obj = JsonSerializer.Serialize(position);
        return Encoding.UTF8.GetBytes(obj);
    }
}