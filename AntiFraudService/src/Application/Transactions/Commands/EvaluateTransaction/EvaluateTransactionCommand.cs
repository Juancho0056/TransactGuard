using System.Text.Json;
using AntiFraudService.Application.Common.Interfaces;
using AntiFraudService.Application.Transactions.Events;
using AntiFraudService.Application.Transactions.Models;
using AntiFraudService.Domain.Policies;
using AntiFraudService.Domain.Ports;
using AntiFraudService.Domain.TransactionEvaluations;
using AntiFraudService.Domain.ValueObjects;
using BuildingBlocks.Application.Abstractions.Data;
using BuildingBlocks.Application.Abstractions.Messaging;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AntiFraudService.Application.Transactions.Commands.EvaluateTransaction;

public sealed record EvaluateTransactionCommand(
    Guid TransactionExternalId,
    Guid SourceAccountId,
    Guid TargetAccountId,
    string TransferType,
    decimal Value,
    DateTimeOffset? OccurredOn = null) : IRequest<EvaluationResultDto>;

internal sealed class EvaluateTransactionCommandHandler : IRequestHandler<EvaluateTransactionCommand, EvaluationResultDto>
{
    private readonly IUtcNowProvider _utcNowProvider;
    private readonly ITransactionsReadPort _transactionsReadPort;
    private readonly IApprovedTransactionsLedger _approvedTransactionsLedger;
    private readonly IApplicationDbContext _context;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ITransactionEvaluationsRepository _transactionEvaluationsRepository;
    private readonly ILogger<EvaluateTransactionCommandHandler> _logger;
    private readonly ITimeZoneProvider _timeZoneProvider;

    public EvaluateTransactionCommandHandler(
        IUtcNowProvider utcNowProvider,
        ITransactionsReadPort transactionsReadPort,
        IApprovedTransactionsLedger approvedTransactionsLedger,
        IApplicationDbContext context,
        IKafkaProducer kafkaProducer,
        ITransactionEvaluationsRepository transactionEvaluationsRepository,
        ITimeZoneProvider timeZoneProvider,
        ILogger<EvaluateTransactionCommandHandler> logger)
    {
        _utcNowProvider = utcNowProvider;
        _transactionsReadPort = transactionsReadPort;
        _approvedTransactionsLedger = approvedTransactionsLedger;
        _context = context;
        _kafkaProducer = kafkaProducer;
        _transactionEvaluationsRepository = transactionEvaluationsRepository;
        _timeZoneProvider = timeZoneProvider;
        _logger = logger;
    }

    public async Task<EvaluationResultDto> Handle(EvaluateTransactionCommand request, CancellationToken cancellationToken)
    {
        var existingEvaluation = await _transactionEvaluationsRepository
            .GetAsync(request.TransactionExternalId, cancellationToken)
            .ConfigureAwait(false);

        if (existingEvaluation is not null)
        {
            _logger.LogInformation(
                "Returning cached antifraud evaluation for transaction {TransactionExternalId}",
                request.TransactionExternalId);

            var reasonCode = string.IsNullOrWhiteSpace(existingEvaluation.ReasonCode)
                ? null
                : ReasonCode.From(existingEvaluation.ReasonCode);

            var cachedResult = new EvaluationResultDto(
                existingEvaluation.TransactionExternalId,
                existingEvaluation.IsApproved,
                reasonCode,
                existingEvaluation.EvaluatedAt);

            var cachedEvent = new TransactionEvaluationIntegrationEvent(
                cachedResult.TransactionExternalId,
                cachedResult.IsApproved,
                cachedResult.Reason,
                cachedResult.EvaluatedAt);

            var cachedPayload = JsonSerializer.Serialize(cachedEvent);

            await _kafkaProducer
                .ProduceAsync(
                    AntiFraudTopics.TransactionEvaluated,
                    request.TransactionExternalId.ToString(),
                    cachedPayload,
                    cancellationToken)
                .ConfigureAwait(false);

            return cachedResult;
        }

        var occurredOn = request.OccurredOn ?? _utcNowProvider.UtcNow;
        var timeZone = _timeZoneProvider.GetTimeZone();
        var occurredOnLocal = TimeZoneInfo.ConvertTime(occurredOn, timeZone);
        var occurredOnDate = DateOnly.FromDateTime(occurredOnLocal.DateTime);
        var sourceAccountId = AccountId.Create(request.SourceAccountId);
        var amount = Money.From(request.Value);
        var context = EvaluationContext.Create(sourceAccountId, amount, occurredOnLocal, occurredOnDate);

        var policies = new IAntiFraudPolicy[]
        {
            SingleTransactionLimitPolicy.CreateDefault(),
            DailyAmountLimitPolicy.CreateDefault(_transactionsReadPort)
        };

        var compositePolicy = new CompositeAntiFraudPolicy(policies);
        var decision = compositePolicy.Evaluate(context);

        var evaluatedAt = occurredOnLocal;

        if (decision.IsApproved)
        {
            await _approvedTransactionsLedger.AddAsync(
                request.TransactionExternalId,
                sourceAccountId,
                amount,
                context.OccurredOnDate,
                evaluatedAt,
                cancellationToken).ConfigureAwait(false);
        }

        var result = new EvaluationResultDto(
            request.TransactionExternalId,
            decision.IsApproved,
            decision.Reason,
            evaluatedAt);

        var createdAt = TimeZoneInfo.ConvertTime(_utcNowProvider.UtcNow, timeZone);

        var evaluationEntity = TransactionEvaluation.Create(
            request.TransactionExternalId,
            decision.IsApproved,
            decision.Reason?.Value,
            evaluatedAt,
            createdAt);

        await _transactionEvaluationsRepository
            .AddAsync(evaluationEntity, cancellationToken)
            .ConfigureAwait(false);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var integrationEvent = new TransactionEvaluationIntegrationEvent(
            result.TransactionExternalId,
            result.IsApproved,
            result.Reason,
            result.EvaluatedAt);

        var payload = JsonSerializer.Serialize(integrationEvent);

        await _kafkaProducer
            .ProduceAsync(AntiFraudTopics.TransactionEvaluated, request.TransactionExternalId.ToString(), payload, cancellationToken)
            .ConfigureAwait(false);

        return result;
    }
}
