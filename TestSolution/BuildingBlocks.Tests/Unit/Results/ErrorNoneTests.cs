using BuildingBlocks.Domain.Results;
using NUnit.Framework;
using Shouldly;

namespace BuildingBlocks.Tests.Unit.Results;

public sealed class ErrorNoneTests
{
    [Test]
    public void None_ShouldHaveEmptyCodeAndMessage()
    {
        Error.None.Code.ShouldBeEmpty();
        Error.None.Message.ShouldBeEmpty();
    }

    [Test]
    public void IsNone_ShouldReturnTrueForNone()
    {
        Error.None.IsNone.ShouldBeTrue();
    }

    [Test]
    public void IsNone_ShouldReturnFalseForDifferentError()
    {
        var error = new Error("code", "message");

        error.IsNone.ShouldBeFalse();
    }
}
