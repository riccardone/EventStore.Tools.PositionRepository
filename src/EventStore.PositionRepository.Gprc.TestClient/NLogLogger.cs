using System;
using NLog;
using EventStore.PositionRepository.Gprc;

namespace EventStore.PositionRepository.TestClient.Gprc
{
    public class NLogLogger : PositionRepository.Gprc.ILogger
    {
        private readonly Logger _logger;

        public NLogLogger(Logger logger)
        {
            _logger = logger;
        }
        public void Info(string message)
        {
            _logger.Info(message);
        }

        public void Warn(string message)
        {
            _logger.Warn(message);
        }

        public void Warn(string message, Exception ex)
        {
            _logger.Warn(message, ex.GetBaseException().Message);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(string message, Exception ex)
        {
            _logger.Error(message, ex.GetBaseException().Message);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }
    }
}
