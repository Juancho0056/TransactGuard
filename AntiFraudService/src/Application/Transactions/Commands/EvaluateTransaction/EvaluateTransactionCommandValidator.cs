using FluentValidation;

namespace AntiFraudService.Application.Transactions.Commands.EvaluateTransaction;

public sealed class EvaluateTransactionCommandValidator : AbstractValidator<EvaluateTransactionCommand>
{
    public EvaluateTransactionCommandValidator()
    {
        RuleFor(command => command.TransactionExternalId)
            .NotEmpty();

        RuleFor(command => command.SourceAccountId)
            .NotEmpty();

        RuleFor(command => command.TargetAccountId)
            .NotEmpty();

        RuleFor(command => command.TransferType)
            .NotEmpty();

        RuleFor(command => command.Value)
            .GreaterThan(0);
    }
}
