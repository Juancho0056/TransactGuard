using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Shouldly;
using Tests.Common.Fakes;
using TransactionService.Application.Transactions.Commands.UpdateTransactionStatus;
using TransactionService.Application.Transactions.Events;
using TransactionService.Domain.Transactions.ValueObjects;
using TransactionService.Tests.Common;

namespace TransactionService.Tests.Application.Transactions.Commands;

public sealed class UpdateTransactionStatusCommandHandlerTests
{
    [Test]
    public async Task Handle_ShouldUpdateTransactionAndPublishEvent_WhenStatusIsApproved()
    {
        var context = CreateContext();
        await using var _ = context;
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var creationUtcProvider = new FakeUtcNowProvider(new DateTimeOffset(2024, 4, 1, 7, 30, 0, TimeSpan.Zero));
        var utcNowProvider = new FakeUtcNowProvider(new DateTimeOffset(2024, 4, 1, 8, 0, 0, TimeSpan.Zero));
        var kafkaProducerMock = new Mock<BuildingBlocks.Application.Abstractions.Messaging.IKafkaProducer>();
        kafkaProducerMock
            .Setup(producer => producer.ProduceAsync(
                TransactionTopics.TransactionsStatus,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var existingTransaction = TransactionService.Domain.Transactions.Transaction.CreatePending(
            TransactionExternalId.From(Guid.NewGuid().ToString()),
            BuildingBlocks.Domain.ValueObjects.AccountId.Create(Guid.NewGuid()),
            BuildingBlocks.Domain.ValueObjects.AccountId.Create(Guid.NewGuid()),
            TransferTypeId.From("PIX"),
            BuildingBlocks.Domain.ValueObjects.Money.From(500m),
            creationUtcProvider,
            timeZoneProvider);

        context.Transactions.Add(existingTransaction);
        await context.SaveChangesAsync();

        var handler = new UpdateTransactionStatusCommandHandler(
            context,
            utcNowProvider,
            kafkaProducerMock.Object,
            timeZoneProvider);

        var command = new UpdateTransactionStatusCommand(
            Guid.Parse(existingTransaction.ExternalId.Value),
            TransactionStatus.Approved,
            "Approved automatically");

        var result = await handler.Handle(command, CancellationToken.None);

        result.TransactionExternalId.ShouldBe(Guid.Parse(existingTransaction.ExternalId.Value));
        result.Status.ShouldBe(TransactionStatus.Approved);
        result.DecisionReason.ShouldBe("Approved automatically");
        result.UpdatedAt.ShouldBe(utcNowProvider.UtcNow);

        existingTransaction.Status.ShouldBe(TransactionStatus.Approved);
        existingTransaction.DecisionReason.ShouldBe("Approved automatically");
        existingTransaction.UpdatedAt.ShouldBe(utcNowProvider.UtcNow);

        kafkaProducerMock.Verify(
            producer => producer.ProduceAsync(
                TransactionTopics.TransactionsStatus,
                existingTransaction.ExternalId.Value,
                It.Is<string>(payload => payload.Contains("Approved")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_ShouldThrowNotFoundException_WhenTransactionDoesNotExist()
    {
        var context = CreateContext();
        await using var _ = context;
        var timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
        var utcNowProvider = new FakeUtcNowProvider(new DateTimeOffset(2024, 4, 1, 8, 0, 0, TimeSpan.Zero));
        var kafkaProducerMock = new Mock<BuildingBlocks.Application.Abstractions.Messaging.IKafkaProducer>();

        var handler = new UpdateTransactionStatusCommandHandler(
            context,
            utcNowProvider,
            kafkaProducerMock.Object,
            timeZoneProvider);

        var command = new UpdateTransactionStatusCommand(Guid.NewGuid(), TransactionStatus.Rejected, "invalid");

        var exception = await Should.ThrowAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));

        exception.Message.ShouldContain("was not found");
        kafkaProducerMock.Verify(
            producer => producer.ProduceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static TestTransactionDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestTransactionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TestTransactionDbContext(options);
    }
}
