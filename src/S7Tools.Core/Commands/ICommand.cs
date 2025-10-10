using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Commands;

/// <summary>
/// Represents a command that can be executed.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the unique identifier for this command type.
    /// </summary>
    string CommandType { get; }
}

/// <summary>
/// Represents a command that returns a result when executed.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the command.</typeparam>
public interface ICommand<TResult> : ICommand
{
}

/// <summary>
/// Represents the result of a command execution.
/// </summary>
public class CommandResult
{
    /// <summary>
    /// Gets a value indicating whether the command executed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the error message if the command failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets additional error details if available.
    /// </summary>
    public string? ErrorDetails { get; init; }

    /// <summary>
    /// Gets the exception that caused the command to fail, if any.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful command result.
    /// </summary>
    /// <returns>A successful command result.</returns>
    public static CommandResult Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed command result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorDetails">Additional error details.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed command result.</returns>
    public static CommandResult Failure(string error, string? errorDetails = null, Exception? exception = null) =>
        new() { IsSuccess = false, Error = error, ErrorDetails = errorDetails, Exception = exception };
}

/// <summary>
/// Represents the result of a command execution with a return value.
/// </summary>
/// <typeparam name="T">The type of the return value.</typeparam>
public class CommandResult<T> : CommandResult
{
    /// <summary>
    /// Gets the result value if the command executed successfully.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Creates a successful command result with a value.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <returns>A successful command result with the specified value.</returns>
    public static CommandResult<T> Success(T value) => new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failed command result with an error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="errorDetails">Additional error details.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A failed command result.</returns>
    public new static CommandResult<T> Failure(string error, string? errorDetails = null, Exception? exception = null) =>
        new() { IsSuccess = false, Error = error, ErrorDetails = errorDetails, Exception = exception };
}
