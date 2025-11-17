using BuildingBlocks.Domain.Results;
using NUnit.Framework;
using Shouldly;

namespace BuildingBlocks.Tests.Unit.Results;

public sealed class ResultSuccessFailureTests
{
    [Test]
    public void Success_ShouldReturnSuccessfulResult()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Test]
    public void Failure_ShouldReturnFailureResultWithError()
    {
        var error = new Error("code", "message");

        var result = Result.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Test]
    public void SuccessOfT_ShouldReturnValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Test]
    public void FailureOfT_ShouldNotExposeValue()
    {
        var error = new Error("code", "message");

        var result = Result<int>.Failure(error);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Test]
    public void Constructor_ShouldThrow_WhenSuccessContainsError()
    {
        Should.Throw<InvalidOperationException>(() => new TestResult(true, new Error("code", "message")));
    }

    [Test]
    public void Constructor_ShouldThrow_WhenFailureContainsNoError()
    {
        Should.Throw<InvalidOperationException>(() => new TestResult(false, Error.None));
    }

    private sealed class TestResult : Result
    {
        public TestResult(bool isSuccess, Error error)
            : base(isSuccess, error)
        {
        }
    }
}
