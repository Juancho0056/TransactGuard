namespace BuildingBlocks.Domain.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("A successful result cannot contain an error.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("A failure result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, Error error, T? value)
        : base(isSuccess, error)
    {
        Value = value!;
    }

    public T Value { get; }

    public static Result<T> Success(T value) => new(true, Error.None, value);

    public static new Result<T> Failure(Error error) => new(false, error, default);
}
