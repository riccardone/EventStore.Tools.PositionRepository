using System;
using NLog;

namespace EventStore.PositionRepository.TestClient
{
    public class NLogPositionRepositoryLogger : IPositionRepositoryLogger
    {
        private readonly Logger _logger;

        public NLogPositionRepositoryLogger(Logger logger)
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
            throw new NotImplementedException();
        }

        public void Warn(string message, string warnMessage)
        {
            _logger.Warn(message, warnMessage);
        }

        public void Error(string message)
        {
            _logger.Error(message);
        }

        public void Error(string message, Exception ex)
        {
            throw new NotImplementedException();
        }

        public void Error(string message, string errorMessage)
        {
            _logger.Error(message, errorMessage);
        }

        public void Debug(string message)
        {
            _logger.Debug(message);
        }
    }
}
