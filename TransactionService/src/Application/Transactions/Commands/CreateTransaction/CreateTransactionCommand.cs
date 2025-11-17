using System.Globalization;
using System.Text.Json;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.ValueObjects;
using BuildingBlocks.Messaging.Outbox;
using TransactionService.Application.Common.Interfaces;
using TransactionService.Application.Transactions.Events;
using TransactionService.Application.Transactions.Models;
using TransactionService.Domain.Transactions;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Application.Transactions.Commands.CreateTransaction;

public sealed record CreateTransactionCommand(
    Guid SourceAccountId,
    Guid TargetAccountId,
    int TransferTypeId,
    decimal Value,
    string? Description = null) : IRequest<TransactionDto>;

internal sealed class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUtcNowProvider _utcNowProvider;
    private readonly ITimeZoneProvider _timeZoneProvider;

    public CreateTransactionCommandHandler(
        IApplicationDbContext context,
        IUtcNowProvider utcNowProvider,
        ITimeZoneProvider timeZoneProvider)
    {
        _context = context;
        _utcNowProvider = utcNowProvider;
        _timeZoneProvider = timeZoneProvider;
    }

    public async Task<TransactionDto> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var externalId = TransactionExternalId.From(Guid.NewGuid().ToString());
        var sourceAccount = AccountId.Create(request.SourceAccountId);
        var targetAccount = AccountId.Create(request.TargetAccountId);
        var transferType = TransferTypeId.From(request.TransferTypeId.ToString(CultureInfo.InvariantCulture));
        var value = Money.From(request.Value);

        var transactionEvent = Transaction.CreatePending(
            externalId,
            sourceAccount,
            targetAccount,
            transferType,
            value,
            _utcNowProvider,
            _timeZoneProvider,
            request.Description);

        _context.Transactions.Add(transactionEvent);
        var transactionExternalId = Guid.Parse(transactionEvent.ExternalId.Value);

        var integrationEvent = new TransactionCreatedIntegrationEvent(
            transactionExternalId,
            transactionEvent.SourceAccountId.Value,
            transactionEvent.TargetAccountId.Value,
            transactionEvent.TransferType.Value,
            transactionEvent.Value.Amount,
            transactionEvent.CreatedAt);

        var payload = JsonSerializer.Serialize(integrationEvent);

        var outboxMessage = OutboxMessage.Create(
            Guid.NewGuid(),
            transactionExternalId,
            nameof(TransactionCreatedIntegrationEvent),
            payload,
            _utcNowProvider.UtcNow);

        _context.OutboxMessages.Add(outboxMessage);

        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new TransactionDto(
            Guid.Parse(transactionEvent.ExternalId.Value),
            transactionEvent.SourceAccountId.Value,
            transactionEvent.TargetAccountId.Value,
            transactionEvent.TransferType.Value,
            transactionEvent.Value.Amount,
            transactionEvent.Status,
            transactionEvent.CreatedAt,
            transactionEvent.UpdatedAt,
            transactionEvent.Description,
            transactionEvent.DecisionReason);
    }
}
