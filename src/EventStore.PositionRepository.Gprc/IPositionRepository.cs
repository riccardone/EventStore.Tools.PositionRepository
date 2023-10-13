using System.Threading.Tasks;
using EventStore.Client;

namespace EventStore.PositionRepository.Gprc
{
    public interface IPositionRepository
    {
        string PositionEventType { get; }
        Position Get();
        void Set(Position position);
    }
}
