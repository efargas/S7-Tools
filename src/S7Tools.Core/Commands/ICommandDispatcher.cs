using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Commands;

/// <summary>
/// Defines a command dispatcher that can execute commands.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Dispatches a command that doesn't return a value.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Dispatches a command that returns a value.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to dispatch.</typeparam>
    /// <typeparam name="TResult">The type of result returned by the command.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    Task<CommandResult<TResult>> DispatchAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResult>;
}
