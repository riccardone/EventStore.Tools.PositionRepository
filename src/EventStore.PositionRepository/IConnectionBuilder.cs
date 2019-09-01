using System;
using EventStore.ClientAPI;

namespace EventStore.PositionRepository
{
    public interface IConnectionBuilder
    {
        Uri ConnectionString { get; }
        ConnectionSettings ConnectionSettings { get; }
        string ConnectionName { get; }
        IEventStoreConnection Build();
    }
}
