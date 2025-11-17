using BuildingBlocks.Domain.Guards;
using NUnit.Framework;
using Shouldly;

namespace BuildingBlocks.Tests.Unit.Primitives;

public sealed class GuardTests
{
    [Test]
    public void AgainstNull_ShouldThrow_WhenValueIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(() => Guard.AgainstNull(null));

        exception.ParamName.ShouldBe("value");
    }

    [Test]
    public void AgainstNull_ShouldNotThrow_WhenValueIsProvided()
    {
        Should.NotThrow(() => Guard.AgainstNull("value"));
    }

    [Test]
    public void AgainstEmptyGuid_ShouldThrow_WhenGuidIsEmpty()
    {
        var exception = Should.Throw<ArgumentException>(() => Guard.AgainstEmptyGuid(Guid.Empty));

        exception.ParamName.ShouldBe("value");
    }

    [Test]
    public void AgainstNegativeOrZero_ShouldThrow_WhenValueIsZeroOrNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.AgainstNegativeOrZero(0));
        Should.Throw<ArgumentOutOfRangeException>(() => Guard.AgainstNegativeOrZero(-1));
    }

    [Test]
    public void AgainstNegativeOrZero_ShouldNotThrow_WhenValueIsPositive()
    {
        Should.NotThrow(() => Guard.AgainstNegativeOrZero(1));
    }
}
