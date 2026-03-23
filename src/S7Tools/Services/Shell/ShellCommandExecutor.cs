using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Shell;

namespace S7Tools.Services.Shell;

/// <summary>
/// Implementation of IShellCommandExecutor that executes commands safely using direct process execution where possible.
/// </summary>
public sealed class ShellCommandExecutor : IShellCommandExecutor
{
    private readonly ILogger<ShellCommandExecutor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellCommandExecutor"/> class.
    /// </summary>
    public ShellCommandExecutor(ILogger<ShellCommandExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ShellCommandResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        return await ExecuteCommandInternalAsync(command, -1, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ShellCommandResult> ExecuteCommandWithTimeoutAsync(string command, int timeoutMs, CancellationToken cancellationToken = default)
    {
        if (timeoutMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeoutMs), "Timeout must be greater than zero.");
        }

        return await ExecuteCommandInternalAsync(command, timeoutMs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<List<int>> GetChildProcessesAsync(int parentPid, CancellationToken cancellationToken = default)
    {
        var childPids = new List<int>();

        try
        {
            // Use pgrep to find child processes on Linux/Unix
            var result = await ExecuteDirectAsync("pgrep", ["-P", parentPid.ToString()], 5000, cancellationToken).ConfigureAwait(false);

            if (result.Success && !string.IsNullOrWhiteSpace(result.Output))
            {
                foreach (string line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(line.Trim(), out int childPid))
                    {
                        childPids.Add(childPid);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get child processes for {ParentPid}", parentPid);
        }

        return childPids;
    }

    /// <inheritdoc />
    public async Task<ShellCommandResult> ExecuteDirectAsync(string fileName, IEnumerable<string> arguments, int timeoutMs = -1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName cannot be null or empty.", nameof(fileName));
        }

        try
        {
            var argsList = arguments?.ToList() ?? new List<string>();
            _logger.LogTrace("Executing direct command: {FileName} {Arguments}", fileName, string.Join(" ", argsList));

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (string arg in argsList)
            {
                startInfo.ArgumentList.Add(arg);
            }

            return await ExecuteProcessAsync(startInfo, timeoutMs, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing direct command: {FileName}", fileName);
            return new ShellCommandResult(false, -1, string.Empty, ex.Message);
        }
    }

    private async Task<ShellCommandResult> ExecuteCommandInternalAsync(string command, int timeoutMs, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));
        }

        try
        {
            bool needsShell = command.Contains('|') || command.Contains('>') || command.Contains('<') || command.Contains('&') || command.Contains(';');

            if (needsShell)
            {
                _logger.LogTrace("Executing shell command via /bin/bash: {Command}", command);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add(command);

                return await ExecuteProcessAsync(startInfo, timeoutMs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                _logger.LogTrace("Executing parsed shell command directly: {Command}", command);

                var args = SplitCommandLine(command);
                if (args.Count == 0)
                {
                    return new ShellCommandResult(false, -1, string.Empty, "Command parsed to empty.");
                }

                string fileName = args[0];
                args.RemoveAt(0);

                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                foreach (string arg in args)
                {
                    startInfo.ArgumentList.Add(arg);
                }

                return await ExecuteProcessAsync(startInfo, timeoutMs, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing shell command: {Command}", command);
            return new ShellCommandResult(false, -1, string.Empty, ex.Message);
        }
    }

    private async Task<ShellCommandResult> ExecuteProcessAsync(ProcessStartInfo startInfo, int timeoutMs, CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) { outputBuilder.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) { errorBuilder.AppendLine(e.Data); } };

        try
        {
            if (!process.Start())
            {
                _logger.LogError("Failed to start process: {FileName}", startInfo.FileName);
                return new ShellCommandResult(false, -1, string.Empty, "Failed to start process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            bool exited;
            if (timeoutMs > 0)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                try
                {
                    await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                    exited = true;
                }
                catch (OperationCanceledException)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Process execution cancelled externally: {FileName}", startInfo.FileName);
                        errorBuilder.AppendLine("Process execution cancelled.");
                    }
                    else
                    {
                        _logger.LogWarning("Process timed out after {Timeout}ms: {FileName}", timeoutMs, startInfo.FileName);
                        errorBuilder.AppendLine($"Process timed out after {timeoutMs}ms.");
                    }

                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true); // Kill entire process tree
                        }
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogWarning(killEx, "Error killing process {Pid}", process.Id);
                    }

                    exited = false;
                }
            }
            else
            {
                try
                {
                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    exited = true;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Process execution cancelled externally: {FileName}", startInfo.FileName);
                    errorBuilder.AppendLine("Process execution cancelled.");

                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true); // Kill entire process tree
                        }
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogWarning(killEx, "Error killing process {Pid}", process.Id);
                    }

                    exited = false;
                }
            }

            int exitCode = exited ? process.ExitCode : -1;
            string stdout = outputBuilder.ToString();
            string stderr = errorBuilder.ToString();

            if (exited)
            {
                _logger.LogTrace("Process completed with exit code {ExitCode}. Output length: {OutLen}, Error length: {ErrLen}",
                    exitCode, stdout.Length, stderr.Length);
            }

            return new ShellCommandResult(exited && exitCode == 0, exitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during process execution: {FileName}", startInfo.FileName);
            return new ShellCommandResult(false, -1, string.Empty, ex.Message);
        }
    }

    /// <summary>
    /// A simple command line parser that honors quotes.
    /// Note: This is a basic implementation and might not cover all bash escaping nuances.
    /// It is intended to prevent trivial shell injections by executing commands directly without a shell.
    /// </summary>
    public static List<string> SplitCommandLine(string commandLine)
    {
        var result = new List<string>();
        var currentArg = new StringBuilder();
        bool inSingleQuote = false;
        bool inDoubleQuote = false;
        bool escapeNext = false;

        for (int i = 0; i < commandLine.Length; i++)
        {
            char c = commandLine[i];

            if (escapeNext)
            {
                currentArg.Append(c);
                escapeNext = false;
                continue;
            }

            if (c == '\\' && !inSingleQuote)
            {
                escapeNext = true;
                continue;
            }

            if (c == '\'' && !inDoubleQuote)
            {
                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (c == '"' && !inSingleQuote)
            {
                inDoubleQuote = !inDoubleQuote;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inSingleQuote && !inDoubleQuote)
            {
                if (currentArg.Length > 0)
                {
                    result.Add(currentArg.ToString());
                    currentArg.Clear();
                }
                continue;
            }

            currentArg.Append(c);
        }

        if (currentArg.Length > 0)
        {
            result.Add(currentArg.ToString());
        }

        return result;
    }
}
