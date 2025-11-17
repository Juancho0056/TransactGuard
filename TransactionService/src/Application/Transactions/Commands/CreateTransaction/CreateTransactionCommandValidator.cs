using FluentValidation;

namespace TransactionService.Application.Transactions.Commands.CreateTransaction;

public sealed class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(command => command.SourceAccountId)
            .NotEmpty();

        RuleFor(command => command.TargetAccountId)
            .NotEmpty();

        RuleFor(command => command.SourceAccountId)
            .NotEqual(command => command.TargetAccountId)
            .WithMessage("Source and target accounts must be different.");

        RuleFor(command => command.TransferTypeId)
            .GreaterThan(0);

        RuleFor(command => command.Value)
            .GreaterThan(0);

        RuleFor(command => command.Description)
            .MaximumLength(512);
    }
}
