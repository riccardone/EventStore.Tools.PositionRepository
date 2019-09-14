using System;

namespace EventStore.PositionRepository
{
    public interface IPositionRepositoryLogger
    {
        void Info(string message);
        void Warn(string message);
        void Warn(string message, Exception ex);
        void Warn(string message, string warnMessage);
        void Error(string message);
        void Error(string message, Exception ex);
        void Error(string message, string errorMessage);
        void Debug(string message);
    }
}
