using System.Text.Json;
using BuildingBlocks.Application.Abstractions.Messaging;
using BuildingBlocks.Domain.Enums;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.Results;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Application.Transactions.Events;
using TransactionService.Application.Transactions.Models;
using TransactionService.Domain.Transactions.Errors;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Application.Transactions.Commands.UpdateTransactionStatus;

public sealed record UpdateTransactionStatusCommand(
    Guid TransactionExternalId,
    TransactionStatus Status,
    string? Reason = null) : IRequest<TransactionStatusDto>;

internal sealed class UpdateTransactionStatusCommandHandler : IRequestHandler<UpdateTransactionStatusCommand, TransactionStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUtcNowProvider _utcNowProvider;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ITimeZoneProvider _timeZoneProvider;

    public UpdateTransactionStatusCommandHandler(
        IApplicationDbContext context,
        IUtcNowProvider utcNowProvider,
        IKafkaProducer kafkaProducer,
        ITimeZoneProvider timeZoneProvider)
    {
        _context = context;
        _utcNowProvider = utcNowProvider;
        _kafkaProducer = kafkaProducer;
        _timeZoneProvider = timeZoneProvider;
    }

    public async Task<TransactionStatusDto> Handle(UpdateTransactionStatusCommand request, CancellationToken cancellationToken)
    {
        var externalId = TransactionExternalId.From(request.TransactionExternalId.ToString());

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.ExternalId == externalId, cancellationToken)
            .ConfigureAwait(false);

        if (transaction is null)
        {
            throw new BuildingBlocks.Application.Exceptions.NotFoundException($"Transaction '{request.TransactionExternalId}' was not found.");
        }

        var result = request.Status switch
        {
            TransactionStatus.Approved => transaction.Approve(_utcNowProvider, _timeZoneProvider, request.Reason),
            TransactionStatus.Rejected => transaction.Reject(_utcNowProvider, _timeZoneProvider, request.Reason ?? string.Empty),
            TransactionStatus.AntiFraudFailed => transaction.MarkAsAntiFraudFailed(_utcNowProvider, _timeZoneProvider, request.Reason),
            _ => Result.Failure(TransactionErrors.InvalidStatus),
        };

        if (result.IsFailure)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var integrationEvent = new TransactionStatusIntegrationEvent(
            Guid.Parse(transaction.ExternalId.Value),
            transaction.Status,
            transaction.DecisionReason,
            transaction.UpdatedAt);

        var payload = JsonSerializer.Serialize(integrationEvent);

        await _kafkaProducer
            .ProduceAsync(TransactionTopics.TransactionsStatus, transaction.ExternalId.Value, payload, cancellationToken)
            .ConfigureAwait(false);

        return new TransactionStatusDto(
            Guid.Parse(transaction.ExternalId.Value),
            transaction.Status,
            transaction.CreatedAt,
            transaction.UpdatedAt,
            transaction.DecisionReason);
    }
}
