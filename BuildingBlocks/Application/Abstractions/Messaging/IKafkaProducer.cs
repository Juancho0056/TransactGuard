using System.Threading;
using System.Threading.Tasks;

namespace BuildingBlocks.Application.Abstractions.Messaging;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string key, string value, CancellationToken cancellationToken = default);
}
