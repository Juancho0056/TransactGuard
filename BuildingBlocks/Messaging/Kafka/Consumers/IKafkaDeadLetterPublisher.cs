using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Messaging.Kafka.Consumers;

public interface IKafkaDeadLetterPublisher<TMessage>
{
    Task PublishAsync(KafkaDeadLetter<TMessage> deadLetter, CancellationToken cancellationToken);
}
