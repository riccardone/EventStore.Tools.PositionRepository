using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace EventStore.PositionRepository
{
    public interface IPositionRepository
    {
        string PositionEventType { get; }
        Position Get();
        void Set(Position position);
        Task Start();
        void Stop();
    }
}
