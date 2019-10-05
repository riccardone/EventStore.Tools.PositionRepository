using System;
using EventStore.ClientAPI;

namespace EventStore.PositionRepository
{
    [Obsolete("This interface will be removed next release in favor of a delegate")]
    public interface IConnectionBuilder
    {
        Uri ConnectionString { get; }
        ConnectionSettings ConnectionSettings { get; }
        string ConnectionName { get; }
        [Obsolete("This interface will be removed next release in favor of a delegate")]
        IEventStoreConnection Build();
    }
}
