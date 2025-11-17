namespace BuildingBlocks.Messaging.Outbox;

public interface IOutboxTopicResolver
{
    string ResolveTopic(string messageType);
}
