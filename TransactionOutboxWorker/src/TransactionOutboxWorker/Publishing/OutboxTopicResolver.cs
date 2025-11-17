using System;
using BuildingBlocks.Messaging.Outbox;
using TransactionService.Application.Transactions.Events;

namespace TransactionOutboxWorker.Publishing;

public sealed class OutboxTopicResolver : IOutboxTopicResolver
{
    private static readonly string TransactionCreatedType = nameof(TransactionCreatedIntegrationEvent);

    public string ResolveTopic(string messageType)
    {
        if (string.Equals(messageType, TransactionCreatedType, StringComparison.Ordinal))
        {
            return TransactionTopics.TransactionsCreated;
        }

        throw new InvalidOperationException($"Unsupported outbox message type '{messageType}'.");
    }
}
