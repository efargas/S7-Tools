using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Core.Interfaces.Shell;

/// <summary>
/// Result of a shell command execution.
/// </summary>
public record ShellCommandResult(bool Success, int ExitCode, string Output, string Error);

/// <summary>
/// Service for executing shell commands and managing processes.
/// </summary>
public interface IShellCommandExecutor
{
    /// <summary>
    /// Executes a command asynchronously and waits for it to complete.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the execution.</returns>
    Task<ShellCommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command with a timeout and waits for it to complete.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="timeoutMs">Timeout in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the execution.</returns>
    Task<ShellCommandResult> ExecuteCommandWithTimeoutAsync(string command, int timeoutMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the IDs of child processes for a given parent process ID.
    /// </summary>
    /// <param name="parentPid">The parent process ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of child process IDs.</returns>
    Task<List<int>> GetChildProcessesAsync(int parentPid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command directly without a shell.
    /// </summary>
    /// <param name="fileName">The executable to run.</param>
    /// <param name="arguments">The arguments to pass to the executable.</param>
    /// <param name="timeoutMs">Timeout in milliseconds (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the execution.</returns>
    Task<ShellCommandResult> ExecuteDirectAsync(string fileName, IEnumerable<string> arguments, int timeoutMs = -1, CancellationToken cancellationToken = default);
}
