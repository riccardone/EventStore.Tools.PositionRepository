using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace EventStore.PositionRepository
{
    public delegate IEventStoreConnection BuildConnection();

    public class PositionRepository : IPositionRepository
    {
        private readonly ILogger _log;
        private readonly string _positionStreamName;
        private readonly BuildConnection _buildConnection;
        private readonly int _interval;
        public string PositionEventType { get; }
        private IEventStoreConnection _connection;
        private static Timer _timer;
        private Position _position = Position.Start;
        private Position _lastSavedPosition = Position.Start;
        private readonly int _maxAge = 0; // 1 week is 604800000

        /// <summary>
        /// PositionRepository tcp client
        /// </summary>
        /// <param name="positionStreamName">The name of the stream containing the saved position</param>
        /// <param name="positionEventType">Set the name for the position event</param>
        /// <param name="buildConnection">to build the eventstore connection client</param>
        /// <param name="logger">Logger</param>
        /// <param name="interval">Define the interval between saving positions</param>
        /// <param name="maxAge">When this is set, the positions will last in the stream for the defined number of seconds, if not set there will only be the last position available in the stream</param>
        public PositionRepository(string positionStreamName, string positionEventType, BuildConnection buildConnection,
            ILogger logger, int interval = 1000, int maxAge = 0)
        {
            _positionStreamName = positionStreamName;
            _buildConnection = buildConnection;
            _interval = interval;
            PositionEventType = positionEventType;
            if (interval <= 0) return;
            _timer = new Timer(interval);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true;
            _log = logger;
            _maxAge = maxAge;
        }

        public PositionRepository(string positionStreamName, string positionEventType, BuildConnection buildConnection,
            int interval = 1000, int maxAge = 0) : this(positionStreamName, positionEventType, buildConnection,
            new SimpleConsoleLogger(nameof(PositionRepository)), interval, maxAge)
        {
        }

        public PositionRepository(string positionStreamName, string positionEventType, IConnectionBuilder connectionBuilder, 
            ILogger logger, int interval = 1000, int maxAge = 0) : this(positionStreamName, positionEventType, connectionBuilder.Build, logger, interval, maxAge)
        {
        }

        public PositionRepository(string positionStreamName, string positionEventType, IConnectionBuilder connectionBuilder,
            int interval = 1000, int maxAge = 0) : this(positionStreamName, positionEventType, connectionBuilder.Build,
            new SimpleConsoleLogger(nameof(PositionRepository)), interval, maxAge)
        {
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_lastSavedPosition.Equals(_position))
                return;
            SavePosition();
        }

        private void SavePosition()
        {
            _connection.AppendToStreamAsync(_positionStreamName, ExpectedVersion.Any,
                new[] { new EventData(Guid.NewGuid(), PositionEventType, true, SerializeObject(_position), null) });
            _lastSavedPosition = _position;
        }

        public async Task Start()
        {
            _connection?.Close();
            _connection = _buildConnection();
            _connection.Connected += _connection_Connected;
            _connection.ErrorOccurred += _connection_ErrorOccurred;
            _connection.Disconnected += _connection_Disconnected;
            await _connection.ConnectAsync();
        }

        private void _connection_Disconnected(object sender, ClientConnectionEventArgs e)
        {
            _log.Warn($"Disconnected '{e.Connection.ConnectionName}'");
            Stop();
            Start();
        }

        public void Stop()
        {
            _connection.Connected -= _connection_Connected;
            _connection.ErrorOccurred -= _connection_ErrorOccurred;
            _connection.Disconnected -= _connection_Disconnected;
            _connection?.Close();
            _timer.Stop();
            _log.Info("PositionRepository stopped");
        }

        private void InitStream()
        {
            try
            {
                if (_maxAge.Equals(0))
                {
                    _connection?.SetStreamMetadataAsync(_positionStreamName, ExpectedVersion.Any,
                        SerializeObject(new Dictionary<string, int> { { "$maxCount", 1 } }));
                }
                else
                {
                    _connection?.SetStreamMetadataAsync(_positionStreamName, ExpectedVersion.Any,
                        SerializeObject(new Dictionary<string, int> { { "$maxAge", _maxAge } }));
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error while initializing stream", ex);
            }
        }

        public Position Get()
        {
            try
            {
                var evts = _connection.ReadStreamEventsBackwardAsync(_positionStreamName, StreamPosition.End, 10, true).Result;
                _position = evts.Events.Any()
                    ? DeserializeObject<Position>(evts.Events[0].OriginalEvent.Data)
                    : Position.Start;
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

        private void _connection_ErrorOccurred(object sender, ClientErrorEventArgs e)
        {
            _log.Warn($"Error while using position repo connection: {e.Exception.GetBaseException().Message}");
        }

        private void _connection_Connected(object sender, ClientConnectionEventArgs e)
        {
            _log?.Info("PositionRepository connected");
            if (_interval > 0)
                _timer.Start();
            InitStream();
        }

        private static byte[] SerializeObject(object obj)
        {
            var jsonObj = JsonConvert.SerializeObject(obj);
            var data = Encoding.UTF8.GetBytes(jsonObj);
            return data;
        }

        private static T DeserializeObject<T>(byte[] data)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.ASCII.GetString(data));
        }
    }
}
