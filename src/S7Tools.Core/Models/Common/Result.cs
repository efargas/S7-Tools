namespace S7Tools.Core.Models;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Implements the Result pattern for functional error handling.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly record struct Result<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Only valid when IsSuccess is true.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message. Only valid when IsFailure is true.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = string.Empty;
        Exception = null;
    }

    private Result(string error, Exception? exception = null)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A successful Result.</returns>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result.</returns>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error message and exception.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed Result.</returns>
    public static Result<T> Failure(string error, Exception exception) => new(error, exception);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A failed Result.</returns>
    public static Result<T> Failure(Exception exception) => new(exception.Message, exception);

    /// <summary>
    /// Maps the success value to a new type using the specified function.
    /// </summary>
    /// <typeparam name="TResult">The target type.</typeparam>
    /// <param name="mapper">The mapping function.</param>
    /// <returns>A Result of the new type.</returns>
    public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
    {
        return IsSuccess && Value is not null
            ? Result<TResult>.Success(mapper(Value))
            : Exception is not null
                ? Result<TResult>.Failure(Error, Exception)
                : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Binds the success value to a new Result using the specified function.
    /// </summary>
    /// <typeparam name="TResult">The target type.</typeparam>
    /// <param name="binder">The binding function.</param>
    /// <returns>A Result of the new type.</returns>
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
    {
        return IsSuccess && Value is not null
            ? binder(Value)
            : Exception is not null
                ? Result<TResult>.Failure(Error, Exception)
                : Result<TResult>.Failure(Error);
    }

    /// <summary>
    /// Executes the specified action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The current Result for chaining.</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && Value is not null)
        {
            action(Value);
        }
        return this;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The current Result for chaining.</returns>
    public Result<T> OnFailure(Action<string> action)
    {
        if (IsFailure)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Gets the value or throws an exception if the result is a failure.
    /// </summary>
    /// <returns>The success value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public T GetValueOrThrow()
    {
        return IsSuccess && Value is not null
            ? Value
            : throw new InvalidOperationException($"Result is in a failure state: {Error}");
    }

    /// <summary>
    /// Gets the value or returns the specified default value if the result is a failure.
    /// </summary>
    /// <param name="defaultValue">The default value to return on failure.</param>
    /// <returns>The success value or the default value.</returns>
    public T GetValueOrDefault(T defaultValue)
    {
        return IsSuccess && Value is not null ? Value : defaultValue;
    }

    /// <summary>
    /// Implicit conversion from T to Result&lt;T&gt;.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    /// <summary>
    /// Implicit conversion from string (error) to Result&lt;T&gt;.
    /// </summary>
    public static implicit operator Result<T>(string error) => Failure(error);
}

/// <summary>
/// Represents the result of an operation that can either succeed or fail without a return value.
/// </summary>
public readonly record struct Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message. Only valid when IsFailure is true.
    /// </summary>
    public string Error { get; }

    /// <summary>
    /// Gets the exception that caused the failure, if any.
    /// </summary>
    public Exception? Exception { get; }

    private Result(bool isSuccess, string error = "", Exception? exception = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful Result.</returns>
    public static Result Success() => new(true);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed Result.</returns>
    public static Result Failure(string error) => new(false, error);

    /// <summary>
    /// Creates a failed result with the specified error message and exception.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed Result.</returns>
    public static Result Failure(string error, Exception exception) => new(false, error, exception);

    /// <summary>
    /// Creates a failed result from an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A failed Result.</returns>
    public static Result Failure(Exception exception) => new(false, exception.Message, exception);

    /// <summary>
    /// Executes the specified action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The current Result for chaining.</returns>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
        {
            action();
        }
        return this;
    }

    /// <summary>
    /// Executes the specified action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>The current Result for chaining.</returns>
    public Result OnFailure(Action<string> action)
    {
        if (IsFailure)
        {
            action(Error);
        }
        return this;
    }

    /// <summary>
    /// Implicit conversion from bool to Result.
    /// </summary>
    public static implicit operator Result(bool success) => success ? Success() : Failure("Operation failed");

    /// <summary>
    /// Implicit conversion from string (error) to Result.
    /// </summary>
    public static implicit operator Result(string error) => Failure(error);
}
