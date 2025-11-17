using FluentValidation;

namespace TransactionService.Application.Transactions.Queries.GetTransactionStatus;

public sealed class GetTransactionStatusQueryValidator : AbstractValidator<GetTransactionStatusQuery>
{
    public GetTransactionStatusQueryValidator()
    {
        RuleFor(query => query.TransactionExternalId)
            .NotEmpty();

        RuleFor(query => query.CreatedAt)
            .NotEmpty();
    }
}
