using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Services;

/// <summary>
/// Implementation of command dispatcher that uses dependency injection to resolve command handlers.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CommandDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandDispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving command handlers.</param>
    /// <param name="logger">The logger instance.</param>
    public CommandDispatcher(IServiceProvider serviceProvider, ILogger<CommandDispatcher> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        if (command == null)
        {
            const string error = "Command cannot be null";
            _logger.LogError(error);
            return CommandResult.Failure(error);
        }

        var commandType = typeof(TCommand);
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

        _logger.LogDebug("Dispatching command: {CommandType}", commandType.Name);

        try
        {
            var handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                var error = $"No handler registered for command type: {commandType.Name}";
                _logger.LogError(error);
                return CommandResult.Failure(error);
            }

            var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<TCommand>.HandleAsync));
            if (handleMethod == null)
            {
                var error = $"HandleAsync method not found on handler for command type: {commandType.Name}";
                _logger.LogError(error);
                return CommandResult.Failure(error);
            }

            var task = (Task<CommandResult>)handleMethod.Invoke(handler, new object[] { command, cancellationToken })!;
            var result = await task.ConfigureAwait(false);

            _logger.LogDebug("Command dispatched successfully: {CommandType}, Success: {IsSuccess}", 
                commandType.Name, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching command: {CommandType}", commandType.Name);
            return CommandResult.Failure("Command dispatch failed", ex.Message, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<CommandResult<TResult>> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>
    {
        if (command == null)
        {
            const string error = "Command cannot be null";
            _logger.LogError(error);
            return CommandResult<TResult>.Failure(error);
        }

        var commandType = typeof(TCommand);
        var resultType = typeof(TResult);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);

        _logger.LogDebug("Dispatching command with result: {CommandType} -> {ResultType}", 
            commandType.Name, resultType.Name);

        try
        {
            var handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                var error = $"No handler registered for command type: {commandType.Name}";
                _logger.LogError(error);
                return CommandResult<TResult>.Failure(error);
            }

            var handleMethod = handlerType.GetMethod(nameof(ICommandHandler<TCommand, TResult>.HandleAsync));
            if (handleMethod == null)
            {
                var error = $"HandleAsync method not found on handler for command type: {commandType.Name}";
                _logger.LogError(error);
                return CommandResult<TResult>.Failure(error);
            }

            var task = (Task<CommandResult<TResult>>)handleMethod.Invoke(handler, new object[] { command, cancellationToken })!;
            var result = await task.ConfigureAwait(false);

            _logger.LogDebug("Command with result dispatched successfully: {CommandType} -> {ResultType}, Success: {IsSuccess}", 
                commandType.Name, resultType.Name, result.IsSuccess);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching command with result: {CommandType} -> {ResultType}", 
                commandType.Name, resultType.Name);
            return CommandResult<TResult>.Failure("Command dispatch failed", ex.Message, ex);
        }
    }
}