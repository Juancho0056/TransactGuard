using BuildingBlocks.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;
using System.Reflection;

namespace TransactionService.Tests.Unit.ValueObjects;

public sealed class AccountIdTests
{
    [Test]
    public void New_ShouldCreateNonEmptyIdentifier()
    {
        var id = AccountId.New();

        id.Value.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void Create_ShouldReturnSameValue()
    {
        var guid = Guid.NewGuid();

        var id = AccountId.Create(guid);

        id.Value.ShouldBe(guid);
        id.ToString().ShouldBe(guid.ToString());
    }

    [Test]
    public void Create_ShouldThrow_WhenGuidIsEmpty()
    {
        var ex = Should.Throw<TargetInvocationException>(
        () => AccountId.Create(Guid.Empty));

        ex.InnerException.ShouldBeOfType<ArgumentException>();
        ex.InnerException!.Message.ShouldContain("The provided value is invalid.");

    }
}
