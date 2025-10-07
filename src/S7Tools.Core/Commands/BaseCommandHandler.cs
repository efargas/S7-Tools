using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Commands;

/// <summary>
/// Base class for command handlers that provides common functionality.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public abstract class BaseCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCommandHandler{TCommand}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected BaseCommandHandler(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            const string error = "Command cannot be null";
            Logger.LogError(error);
            return CommandResult.Failure(error);
        }

        var commandType = command.GetType().Name;
        Logger.LogDebug("Starting execution of command: {CommandType}", commandType);

        try
        {
            var result = await ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
            
            if (result.IsSuccess)
            {
                Logger.LogDebug("Successfully executed command: {CommandType}", commandType);
            }
            else
            {
                Logger.LogWarning("Command execution failed: {CommandType}, Error: {Error}", 
                    commandType, result.Error);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Logger.LogInformation("Command execution was cancelled: {CommandType}", commandType);
            return CommandResult.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled exception in command handler: {CommandType}", commandType);
            return CommandResult.Failure("An unexpected error occurred", ex.Message, ex);
        }
    }

    /// <summary>
    /// Executes the command logic.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    protected abstract Task<CommandResult> ExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the command before execution.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A command result indicating whether the command is valid.</returns>
    protected virtual CommandResult ValidateCommand(TCommand command)
    {
        // Default implementation - override in derived classes for specific validation
        return CommandResult.Success();
    }

    /// <summary>
    /// Executes an operation with comprehensive error handling and logging.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    protected async Task<CommandResult> ExecuteWithLoggingAsync(
        Func<Task<CommandResult>> operation,
        string operationName)
    {
        try
        {
            Logger.LogDebug("Starting operation: {OperationName}", operationName);
            var result = await operation().ConfigureAwait(false);
            
            if (result.IsSuccess)
            {
                Logger.LogDebug("Completed operation successfully: {OperationName}", operationName);
            }
            else
            {
                Logger.LogWarning("Operation failed: {OperationName}, Error: {Error}", 
                    operationName, result.Error);
            }
                
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in operation: {OperationName}", operationName);
            return CommandResult.Failure($"Operation '{operationName}' failed", ex.Message, ex);
        }
    }
}

/// <summary>
/// Base class for command handlers that return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResult">The type of result returned by the command.</typeparam>
public abstract class BaseCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseCommandHandler{TCommand, TResult}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    protected BaseCommandHandler(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            const string error = "Command cannot be null";
            Logger.LogError(error);
            return CommandResult<TResult>.Failure(error);
        }

        var commandType = command.GetType().Name;
        Logger.LogDebug("Starting execution of command: {CommandType}", commandType);

        try
        {
            var result = await ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
            
            if (result.IsSuccess)
            {
                Logger.LogDebug("Successfully executed command: {CommandType}", commandType);
            }
            else
            {
                Logger.LogWarning("Command execution failed: {CommandType}, Error: {Error}", 
                    commandType, result.Error);
            }

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Logger.LogInformation("Command execution was cancelled: {CommandType}", commandType);
            return CommandResult<TResult>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unhandled exception in command handler: {CommandType}", commandType);
            return CommandResult<TResult>.Failure("An unexpected error occurred", ex.Message, ex);
        }
    }

    /// <summary>
    /// Executes the command logic.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    protected abstract Task<CommandResult<TResult>> ExecuteAsync(TCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Executes an operation with comprehensive error handling and logging.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="operationName">The name of the operation for logging purposes.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    protected async Task<CommandResult<TResult>> ExecuteWithLoggingAsync(
        Func<Task<CommandResult<TResult>>> operation,
        string operationName)
    {
        try
        {
            Logger.LogDebug("Starting operation: {OperationName}", operationName);
            var result = await operation().ConfigureAwait(false);
            
            if (result.IsSuccess)
            {
                Logger.LogDebug("Completed operation successfully: {OperationName}", operationName);
            }
            else
            {
                Logger.LogWarning("Operation failed: {OperationName}, Error: {Error}", 
                    operationName, result.Error);
            }
                
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception in operation: {OperationName}", operationName);
            return CommandResult<TResult>.Failure($"Operation '{operationName}' failed", ex.Message, ex);
        }
    }
}