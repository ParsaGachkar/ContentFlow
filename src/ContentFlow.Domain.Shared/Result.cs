namespace ContentFlow.Domain.Shared;

/// <summary>
/// Represents the outcome of an operation that carries no value.
/// Business-rule violations are reported via <see cref="Fail(Error)"/> — never via exceptions.
/// Exceptions are reserved for programming errors (e.g. null arguments).
/// </summary>
public class Result
{
    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error when <see cref="IsFailure"/>; otherwise <see langword="null"/>.</summary>
    public Error? Error { get; }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error when the operation failed; otherwise <see langword="null"/>.</param>
    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Creates a successful result.</summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, null);

    /// <summary>Creates a failed result.</summary>
    /// <param name="error">The failure error.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result Fail(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, error);
    }
}

/// <summary>
/// Represents the outcome of an operation that produces a value of type <typeparamref name="T"/>.
/// A <see cref="Result{T}"/> converts implicitly to <see cref="Result"/> via the built-in
/// reference conversion (C# forbids user-defined conversions to a base type), so no
/// <c>op_Implicit</c> operator is declared: <c>Result result = typedResult;</c> just works.
/// </summary>
/// <typeparam name="T">The value type produced on success.</typeparam>
public sealed class Result<T> : Result
{
    /// <summary>Gets the value when <see cref="Result.IsSuccess"/>; otherwise <c>default</c>.</summary>
    public T? Value { get; }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="error">The error when the operation failed; otherwise <see langword="null"/>.</param>
    /// <param name="value">The value when the operation succeeded.</param>
    private Result(bool isSuccess, Error? error, T? value)
        : base(isSuccess, error)
    {
        Value = value;
    }

    /// <summary>Creates a successful result carrying <paramref name="value"/>.</summary>
    /// <param name="value">The produced value.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success(T value) => new(true, null, value);

    /// <summary>Creates a failed result carrying no value.</summary>
    /// <param name="error">The failure error.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
    public static new Result<T> Fail(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(false, error, default);
    }
}
