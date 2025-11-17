using FluentValidation;

namespace TransactionService.Application.Transactions.Queries.GetAccountDailyTotal;

public sealed class GetAccountDailyTotalQueryValidator : AbstractValidator<GetAccountDailyTotalQuery>
{
    public GetAccountDailyTotalQueryValidator()
    {
        RuleFor(query => query.SourceAccountId)
            .NotEmpty();
    }
}
