using EventStore.Client;
using KurrentDB.Client;

namespace EventStore.PositionRepository.Gprc;

public interface IPositionRepository
{
    string PositionEventType { get; }
    Position Get();
    void Set(Position position);
}