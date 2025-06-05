using KurrentDB.Client;

namespace EventStore.PositionRepository.Gprc;

public interface IPositionRepository
{
    string PositionEventType { get; }
    Position Get();
    bool TryGet(out Position position);
    void Set(Position position);
}