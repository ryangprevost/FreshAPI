namespace FreshApi.Domain.Common;

/// <summary>
/// Represents the outcome of an operation without a return value.
/// Prefer returning <see cref="Result"/> / <see cref="Result{T}"/>
/// over throwing exceptions for expected failures (validation, not found, conflicts).
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, error);
    }
}

/// <summary>
/// Represents the outcome of an operation with a return value of type <typeparamref name="T"/>.
/// </summary>
public sealed class Result<T> : Result
{
    private Result(T value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T Value { get; }

    public static Result<T> Success(T value) => new(value, true, Error.None);

    public static new Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(default!, false, error);
    }

    public static implicit operator Result<T>(T value) => Success(value);
}
