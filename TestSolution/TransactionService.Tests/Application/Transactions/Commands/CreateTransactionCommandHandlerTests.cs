using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using Tests.Common.Fakes;
using TransactionService.Application.Transactions.Commands.CreateTransaction;
using TransactionService.Application.Transactions.Events;
using TransactionService.Tests.Common;

namespace TransactionService.Tests.Application.Transactions.Commands;

public sealed class CreateTransactionCommandHandlerTests
{
    [Test]
    public async Task Handle_ShouldPersistTransactionAndOutboxMessage()
    {
        var options = new DbContextOptionsBuilder<TestTransactionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new TestTransactionDbContext(options);
        var utcNowProvider = new FakeUtcNowProvider(new DateTimeOffset(2024, 3, 10, 12, 0, 0, TimeSpan.Zero));
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var handler = new CreateTransactionCommandHandler(context, utcNowProvider, timeZoneProvider);

        var command = new CreateTransactionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            1500m,
            "Salary payment");

        var result = await handler.Handle(command, CancellationToken.None);

        var transaction = context.Transactions.Single();
        var outboxMessage = context.OutboxMessages.Single();

        transaction.SourceAccountId.Value.ShouldBe(command.SourceAccountId);
        transaction.TargetAccountId.Value.ShouldBe(command.TargetAccountId);
        transaction.Value.Amount.ShouldBe(command.Value);
        transaction.Status.ShouldBe(BuildingBlocks.Domain.Enums.TransactionStatus.Pending);
        transaction.Description.ShouldBe(command.Description);
        transaction.CreatedAt.ShouldBe(utcNowProvider.UtcNow);
        transaction.UpdatedAt.ShouldBe(utcNowProvider.UtcNow);

        outboxMessage.AggregateId.ShouldBe(result.TransactionExternalId);
        outboxMessage.Type.ShouldBe(nameof(TransactionCreatedIntegrationEvent));
        outboxMessage.OccurredOnUtc.ShouldBe(utcNowProvider.UtcNow);

        result.TransactionExternalId.ShouldBe(Guid.Parse(transaction.ExternalId.Value));
        result.Value.ShouldBe(command.Value);
        result.SourceAccountId.ShouldBe(command.SourceAccountId);
        result.TargetAccountId.ShouldBe(command.TargetAccountId);
        result.Status.ShouldBe(transaction.Status);
        result.Description.ShouldBe(command.Description);
    }
}
