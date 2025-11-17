using BuildingBlocks.Domain.Enums;

namespace TransactionService.Application.Transactions.Commands.UpdateTransactionStatus;

public sealed class UpdateTransactionStatusCommandValidator : AbstractValidator<UpdateTransactionStatusCommand>
{
    public UpdateTransactionStatusCommandValidator()
    {
        RuleFor(command => command.TransactionExternalId)
            .NotEmpty();

        RuleFor(command => command.Status)
            .IsInEnum()
            .Must(status => status is TransactionStatus.Approved or TransactionStatus.Rejected or TransactionStatus.AntiFraudFailed)
            .WithMessage("Only the statuses Approved, Rejected, or AntiFraudFailed are allowed.");

        When(command => command.Status == TransactionStatus.Approved, () =>
        {
            RuleFor(command => command.Reason)
                .MaximumLength(256);
        });

        When(command => command.Status == TransactionStatus.Rejected, () =>
        {
            RuleFor(command => command.Reason)
                .NotEmpty()
                .WithMessage("A rejection reason is required when the transaction is rejected.")
                .MaximumLength(256);
        });

        When(command => command.Status == TransactionStatus.AntiFraudFailed, () =>
        {
            RuleFor(command => command.Reason)
                .MaximumLength(256);
        });
    }
}
