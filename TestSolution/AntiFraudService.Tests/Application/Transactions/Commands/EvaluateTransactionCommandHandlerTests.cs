using AntiFraudService.Application.Common.Interfaces;
using AntiFraudService.Application.Transactions.Commands.EvaluateTransaction;
using AntiFraudService.Application.Transactions.Events;
using AntiFraudService.Domain.Ports;
using AntiFraudService.Domain.TransactionEvaluations;
using AntiFraudService.Domain.ValueObjects;
using BuildingBlocks.Application.Abstractions.Data;
using BuildingBlocks.Application.Abstractions.Messaging;
using BuildingBlocks.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using Tests.Common.Fakes;

namespace AntiFraudService.Tests.Application.Transactions.Commands;

public sealed class EvaluateTransactionCommandHandlerTests
{
    private Mock<ITransactionsReadPort> _transactionsReadPortMock = null!;
    private Mock<IApprovedTransactionsLedger> _approvedTransactionsLedgerMock = null!;
    private Mock<IApplicationDbContext> _applicationDbContextMock = null!;
    private Mock<IKafkaProducer> _kafkaProducerMock = null!;
    private Mock<ITransactionEvaluationsRepository> _transactionEvaluationsRepositoryMock = null!;
    private Mock<ILogger<EvaluateTransactionCommandHandler>> _loggerMock = null!;
    private FakeUtcNowProvider _utcNowProvider = null!;
    private FakeTimeZoneProvider _timeZoneProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _transactionsReadPortMock = new Mock<ITransactionsReadPort>();
        _approvedTransactionsLedgerMock = new Mock<IApprovedTransactionsLedger>();
        _applicationDbContextMock = new Mock<IApplicationDbContext>();
        _kafkaProducerMock = new Mock<IKafkaProducer>();
        _transactionEvaluationsRepositoryMock = new Mock<ITransactionEvaluationsRepository>();
        _loggerMock = new Mock<ILogger<EvaluateTransactionCommandHandler>>();
        _utcNowProvider = new FakeUtcNowProvider(new DateTimeOffset(2024, 1, 15, 9, 30, 0, TimeSpan.Zero));
        _timeZoneProvider = new FakeTimeZoneProvider(TimeZoneInfo.Utc);
    }

    [Test]
    public async Task Handle_ShouldReturnCachedEvaluation_WhenEvaluationAlreadyExists()
    {
        var transactionExternalId = Guid.NewGuid();
        var evaluatedAt = new DateTimeOffset(2024, 1, 10, 10, 0, 0, TimeSpan.Zero);
        var existingEvaluation = TransactionEvaluation.Create(
            transactionExternalId,
            false,
            ReasonCode.SingleLimitExceeded.Value,
            evaluatedAt,
            evaluatedAt);

        _transactionEvaluationsRepositoryMock
            .Setup(repository => repository.GetAsync(transactionExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEvaluation);

        _kafkaProducerMock
            .Setup(producer => producer.ProduceAsync(
                AntiFraudTopics.TransactionEvaluated,
                transactionExternalId.ToString(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new EvaluateTransactionCommand(
            transactionExternalId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "PIX",
            500m);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.TransactionExternalId.ShouldBe(transactionExternalId);
        result.IsApproved.ShouldBeFalse();
        result.Reason.ShouldNotBeNull();
        result.Reason.Value.ShouldBe(ReasonCode.SingleLimitExceeded.Value);
        result.EvaluatedAt.ShouldBe(evaluatedAt);

        _approvedTransactionsLedgerMock.Verify(
            ledger => ledger.AddAsync(
                It.IsAny<Guid>(),
                It.IsAny<AccountId>(),
                It.IsAny<Money>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _transactionEvaluationsRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<TransactionEvaluation>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _applicationDbContextMock.Verify(
            context => context.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Handle_ShouldPersistEvaluationAndPublishEvent_WhenDecisionIsApproved()
    {
        var transactionExternalId = Guid.NewGuid();
        var sourceAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        _transactionEvaluationsRepositoryMock
            .Setup(repository => repository.GetAsync(transactionExternalId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionEvaluation?)null);

        _transactionsReadPortMock
            .Setup(port => port.GetTotalAmountByAccountOnDate(It.IsAny<AccountId>(), It.IsAny<DateOnly>()))
            .Returns(Money.From(0));

        _approvedTransactionsLedgerMock
            .Setup(ledger => ledger.AddAsync(
                transactionExternalId,
                It.IsAny<AccountId>(),
                It.IsAny<Money>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _transactionEvaluationsRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<TransactionEvaluation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _applicationDbContextMock
            .Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _kafkaProducerMock
            .Setup(producer => producer.ProduceAsync(
                AntiFraudTopics.TransactionEvaluated,
                transactionExternalId.ToString(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new EvaluateTransactionCommand(
            transactionExternalId,
            sourceAccountId,
            targetAccountId,
            "PIX",
            100m);

        var handler = CreateHandler();

        var result = await handler.Handle(command, CancellationToken.None);

        result.TransactionExternalId.ShouldBe(transactionExternalId);
        result.IsApproved.ShouldBeTrue();
        result.Reason.ShouldBeNull();
        result.EvaluatedAt.ShouldBe(_utcNowProvider.UtcNow);

        _transactionsReadPortMock.Verify(
            port => port.GetTotalAmountByAccountOnDate(
                It.Is<AccountId>(account => account.Value == sourceAccountId),
                It.IsAny<DateOnly>()),
            Times.Once);

        _approvedTransactionsLedgerMock.Verify(
            ledger => ledger.AddAsync(
                transactionExternalId,
                It.Is<AccountId>(account => account.Value == sourceAccountId),
                It.Is<Money>(money => money.Amount == 100m),
                It.IsAny<DateOnly>(),
                It.Is<DateTimeOffset>(timestamp => timestamp == _utcNowProvider.UtcNow),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _transactionEvaluationsRepositoryMock.Verify(
            repository => repository.AddAsync(
                It.Is<TransactionEvaluation>(evaluation =>
                    evaluation.TransactionExternalId == transactionExternalId &&
                    evaluation.IsApproved),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _applicationDbContextMock.Verify(
            context => context.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _kafkaProducerMock.Verify(
            producer => producer.ProduceAsync(
                AntiFraudTopics.TransactionEvaluated,
                transactionExternalId.ToString(),
                It.Is<string>(payload => payload.Contains(transactionExternalId.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private EvaluateTransactionCommandHandler CreateHandler()
    {
        return new EvaluateTransactionCommandHandler(
            _utcNowProvider,
            _transactionsReadPortMock.Object,
            _approvedTransactionsLedgerMock.Object,
            _applicationDbContextMock.Object,
            _kafkaProducerMock.Object,
            _transactionEvaluationsRepositoryMock.Object,
            _timeZoneProvider,
            _loggerMock.Object);
    }
}
