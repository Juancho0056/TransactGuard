using NUnit.Framework;
using Shouldly;
using TransactionService.Domain.Transactions.ValueObjects;

namespace TransactionService.Tests.Unit.ValueObjects;

public sealed class TransferTypeIdTests
{
    [Test]
    public void From_ShouldNormalizeValue()
    {
        var value = TransferTypeId.From(" wire  ");

        value.Value.ShouldBe("WIRE");
        value.ToString().ShouldBe("WIRE");
    }

    [Test]
    public void From_ShouldThrow_WhenValueIsEmpty()
    {
        Should.Throw<ArgumentException>(() => TransferTypeId.From(" "));
    }
}
