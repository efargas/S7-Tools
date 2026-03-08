using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Shell;

namespace S7Tools.Services.Shell;

/// <summary>
/// Implementation of IShellCommandExecutor that executes commands using /bin/bash -c on Linux.
/// </summary>
public sealed class ShellCommandExecutor : IShellCommandExecutor
{
    private readonly ILogger<ShellCommandExecutor> _logger;

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
            _logger.LogTrace("Executing direct command: {FileName} {Arguments}", fileName, string.Join(" ", arguments));

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (string arg in arguments)
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
            _logger.LogTrace("Executing shell command: {Command}", command);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing shell command: {Command}", command);
            return new ShellCommandResult(false, -1, string.Empty, ex.Message);
        }
    }

    private async Task<ShellCommandResult> ExecuteProcessAsync(ProcessStartInfo startInfo, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process { StartInfo = startInfo };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

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
                    _logger.LogWarning("Process timed out after {Timeout}ms: {FileName}", timeoutMs, startInfo.FileName);
                    errorBuilder.AppendLine($"Process timed out after {timeoutMs}ms.");

                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true); // Kill entire process tree
                        }
                    }
                    catch (Exception killEx)
                    {
                        _logger.LogWarning(killEx, "Error killing timed out process {Pid}", process.Id);
                    }

                    exited = false;
                }
            }
            else
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                exited = true;
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
}
