using BuildingBlocks.Domain.Enums;
using BuildingBlocks.Domain.Guards;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.ValueObjects;
using TransactionService.Domain.Transactions.Errors;
using TransactionService.Domain.Transactions.Policies;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Domain.Transactions;

public sealed class Transaction : AggregateRoot
{
    private Transaction(
        TransactionId transactionId,
        TransactionExternalId externalId,
        AccountId sourceAccountId,
        AccountId targetAccountId,
        TransferTypeId transferType,
        Money value,
        DateTimeOffset createdAt,
        string? description = null)
        : base(transactionId.Value)
    {
        Guard.AgainstNull(externalId, parameterName: nameof(externalId));
        Guard.AgainstNull(sourceAccountId, parameterName: nameof(sourceAccountId));
        Guard.AgainstNull(targetAccountId, parameterName: nameof(targetAccountId));
        Guard.AgainstNull(transferType, parameterName: nameof(transferType));
        Guard.AgainstNull(value, parameterName: nameof(value));

        if (value.Amount <= 0)
        {
            throw new InvalidOperationException(TransactionErrors.InvalidValue.Message);
        }

        ExternalId = externalId;
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        TransferType = transferType;
        Value = value;
        Status = TransactionStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        Description = description;
        TransactionStatePolicy.EnsureAccountsAreDifferent(sourceAccountId, targetAccountId);
        RaiseDomainEvent(new TransactionCreated(transactionId, value, createdAt));
    }

    private Transaction()
    {
    }

    public TransactionId TransactionId => TransactionId.Create(Id);

    public TransactionExternalId ExternalId { get; private init; } = null!;

    public AccountId SourceAccountId { get; private init; } = null!;

    public AccountId TargetAccountId { get; private init; } = null!;

    public TransferTypeId TransferType { get; private init; } = null!;

    public Money Value { get; private init; } = null!;

    public TransactionStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public string? Description { get; private set; }

    public string? DecisionReason { get; private set; }

    public static Transaction CreatePending(
        TransactionExternalId externalId,
        AccountId sourceAccountId,
        AccountId targetAccountId,
        TransferTypeId transferType,
        Money value,
        IUtcNowProvider utcNowProvider,
        ITimeZoneProvider timeZoneProvider,
        string? description = null)
    {
        Guard.AgainstNull(utcNowProvider, parameterName: nameof(utcNowProvider));
        Guard.AgainstNull(timeZoneProvider, parameterName: nameof(timeZoneProvider));

        var transactionId = TransactionId.New();
        var createdAt = ConvertToLocal(utcNowProvider.UtcNow, timeZoneProvider);

        return new Transaction(
            transactionId,
            externalId,
            sourceAccountId,
            targetAccountId,
            transferType,
            value,
            createdAt,
            description);
    }

    public Result Approve(IUtcNowProvider utcNowProvider, ITimeZoneProvider timeZoneProvider, string? reason = null)
    {
        Guard.AgainstNull(utcNowProvider, parameterName: nameof(utcNowProvider));
        Guard.AgainstNull(timeZoneProvider, parameterName: nameof(timeZoneProvider));

        var normalizedReason = NormalizeOptionalReason(reason);

        //if (Status == TransactionStatus.Approved)
        //{
        //    UpdateDecisionReason(normalizedReason);
        //    return Result.Success();
        //}

        if (!TransactionStatePolicy.CanTransition(Status, TransactionStatus.Approved))
        {
            return Result.Failure(TransactionErrors.AlreadyFinalized);
        }

        var normalizedDecidedAt = ConvertToLocal(utcNowProvider.UtcNow, timeZoneProvider);

        Status = TransactionStatus.Approved;
        UpdatedAt = normalizedDecidedAt;
        DecisionReason = normalizedReason;
        RaiseDomainEvent(new TransactionApproved(TransactionId, normalizedDecidedAt, DecisionReason));
        return Result.Success();
    }

    public Result Reject(IUtcNowProvider utcNowProvider, ITimeZoneProvider timeZoneProvider, string reason)
    {
        Guard.AgainstNull(utcNowProvider, parameterName: nameof(utcNowProvider));
        Guard.AgainstNull(timeZoneProvider, parameterName: nameof(timeZoneProvider));
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(TransactionErrors.InvalidReason);
        }

        var normalizedReason = reason.Trim();

        //if (Status == TransactionStatus.Rejected)
        //{
        //    UpdateDecisionReason(normalizedReason);
        //    return Result.Success();
        //}

        if (!TransactionStatePolicy.CanTransition(Status, TransactionStatus.Rejected))
        {
            return Result.Failure(TransactionErrors.AlreadyFinalized);
        }

        var normalizedDecidedAt = ConvertToLocal(utcNowProvider.UtcNow, timeZoneProvider);

        Status = TransactionStatus.Rejected;
        UpdatedAt = normalizedDecidedAt;
        DecisionReason = normalizedReason;
        RaiseDomainEvent(new TransactionRejected(TransactionId, normalizedDecidedAt, DecisionReason));
        return Result.Success();
    }

    public Result MarkAsAntiFraudFailed(IUtcNowProvider utcNowProvider, ITimeZoneProvider timeZoneProvider, string? reason)
    {
        Guard.AgainstNull(utcNowProvider, parameterName: nameof(utcNowProvider));
        Guard.AgainstNull(timeZoneProvider, parameterName: nameof(timeZoneProvider));

        var normalizedReason = NormalizeOptionalReason(reason);

        if (Status == TransactionStatus.AntiFraudFailed)
        {
            UpdateDecisionReason(normalizedReason);
            return Result.Success();
        }

        if (!TransactionStatePolicy.CanTransition(Status, TransactionStatus.AntiFraudFailed))
        {
            return Result.Failure(TransactionErrors.AlreadyFinalized);
        }

        var failedAt = ConvertToLocal(utcNowProvider.UtcNow, timeZoneProvider);

        Status = TransactionStatus.AntiFraudFailed;
        UpdatedAt = failedAt;
        DecisionReason = normalizedReason;
        RaiseDomainEvent(new TransactionMarkedAsAntiFraudFailed(TransactionId, failedAt, DecisionReason));
        return Result.Success();
    }

    private static string? NormalizeOptionalReason(string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    private void UpdateDecisionReason(string? reason)
    {
        if (DecisionReason == reason)
        {
            return;
        }

        DecisionReason = reason;
    }

    private static DateTimeOffset ConvertToLocal(DateTimeOffset timestamp, ITimeZoneProvider timeZoneProvider)
    {
        var timeZone = timeZoneProvider.GetTimeZone();
        return TimeZoneInfo.ConvertTime(timestamp, timeZone);
    }
}
