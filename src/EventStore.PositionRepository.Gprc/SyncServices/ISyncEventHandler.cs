using System.Text.Json.Nodes;

namespace EventStore.PositionRepository.Gprc.SyncServices;

public interface ISyncEventHandler
{
    string EventType { get; }
    void Handle(JsonNode data, JsonNode metadata);
}
