using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Commands;

/// <summary>
/// Defines a handler for commands that don't return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handles the specified command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    Task<CommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for commands that return a value.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResult">The type of result returned by the command.</typeparam>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Handles the specified command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with the command result.</returns>
    Task<CommandResult<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}