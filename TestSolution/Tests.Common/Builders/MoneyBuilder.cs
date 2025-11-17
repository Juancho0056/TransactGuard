using BuildingBlocks.Domain.ValueObjects;

namespace Tests.Common.Builders;

public sealed class MoneyBuilder
{
    private decimal _amount = 1m;

    public MoneyBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public Money Build() => Money.From(_amount);
}
