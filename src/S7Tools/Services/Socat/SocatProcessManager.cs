using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Services.Shell;
using S7Tools.Extensions;

namespace S7Tools.Services.Socat;

/// <summary>
/// Manages socat process lifecycle (start, stop, monitor).
/// Single Responsibility: Process management.
/// </summary>
public partial class SocatProcessManager : IDisposable
{
    private readonly ILogger<SocatProcessManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IShellCommandExecutor _shellExecutor;
    private readonly Dictionary<int, Process> _activeProcesses = new();
    private readonly Dictionary<int, Timer> _processMonitors = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public SocatProcessManager(
        ILogger<SocatProcessManager> logger,
        ITimeProvider timeProvider,
        IShellCommandExecutor shellExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _shellExecutor = shellExecutor ?? throw new ArgumentNullException(nameof(shellExecutor));
    }

    /// <summary>
    /// Event raised when a process exits.
    /// </summary>
    public event EventHandler<ProcessExitedEventArgs>? ProcessExited;

    /// <summary>
    /// Starts a socat process with the given command.
    /// </summary>
    public async Task<SocatProcessInfo> StartProcessAsync(
        string command,
        SocatConfiguration configuration,
        string serialDevice,
        SocatProfile? profile,
        ILogger? protocolLogger,
        ILogger? processLogger,
        bool captureOutput = true,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Parse command - prefer direct socat invocation
            string fileName = "socat";
            string arguments;
            string trimmed = command.Trim();

            if (trimmed.StartsWith("socat ", StringComparison.OrdinalIgnoreCase))
            {
                arguments = trimmed[6..].TrimStart();
            }
            else if (string.Equals(trimmed, "socat", StringComparison.OrdinalIgnoreCase))
            {
                arguments = string.Empty;
            }
            else
            {
                throw new ValidationException("Command", "Only socat commands are allowed to be executed.");
            }

            // Build process start info
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = captureOutput,
                CreateNoWindow = true
            };

            _logger.LogInformation("Executing: {Command}", command);

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };

            int processId = 0;
            StringBuilder? outputBuilder = null;
            StringBuilder? errorBuilder = null;

            if (captureOutput)
            {
                outputBuilder = new StringBuilder();
                errorBuilder = new StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        outputBuilder!.AppendLine(e.Data);
                        _logger.LogTrace("Socat output: {Output}", e.Data);
                        processLogger?.LogDebug("socat[{ProcessId}] {Output}", processId, e.Data);
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (e.Data != null)
                    {
                        errorBuilder!.AppendLine(e.Data);

                        // Check if this is hex dump output (protocol data)
                        string trimmedData = e.Data.TrimEnd();
                        bool isHexDumpLine =
                            trimmedData.Contains("< ") ||
                            trimmedData.Contains("> ") ||
                            (trimmedData.Contains("0x") && trimmedData.Length > 15) ||
                            trimmedData.Trim() == "--" ||
                            HexDumpRegex().IsMatch(trimmedData);

                        if (isHexDumpLine && protocolLogger != null)
                        {
                            // Route hex dump to protocol logger
                            string cleanOutput = SocatLogTimestampRegex().Replace(e.Data, string.Empty);
                            protocolLogger.LogDebug("{HexData}", cleanOutput);
                        }
                        else
                        {
                            // Regular error/info output
                            string cleanMessage = SocatLogTimestampRegex().Replace(e.Data, string.Empty);
                            processLogger?.LogInformation("socat[{ProcessId}] {Message}", processId, cleanMessage);
                        }
                    }
                };
            }

            // Set up process exit handler
            process.Exited += (sender, args) =>
            {
                Task.Run(async () =>
                {
                    await _semaphore.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        int pid = process.Id;
                        _logger.LogInformation("Socat process {ProcessId} exited with code {ExitCode}",
                            pid, process.ExitCode);

                        // Clean up references
                        _activeProcesses.Remove(pid);
                        if (_processMonitors.TryGetValue(pid, out Timer? monitor))
                        {
                            monitor.Dispose();
                            _processMonitors.Remove(pid);
                        }

                        // Raise event for facade to handle
                        ProcessExited?.Invoke(this, new ProcessExitedEventArgs(pid, process.ExitCode));
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
            };

            // Start process
            process.Start();
            processId = process.Id;

            if (captureOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            // Wait briefly to detect immediate failures
            await Task.Delay(150, cancellationToken).ConfigureAwait(false);

            if (process.HasExited)
            {
                int exitCode = process.ExitCode;
                string? stderr = captureOutput ? errorBuilder?.ToString() : string.Empty;
                throw new ConnectionException(
                    $"{configuration.TcpHost}:{configuration.TcpPort}",
                    "Socat",
                    $"Socat process exited immediately with code {exitCode}. {stderr}");
            }

            // Create process info
            var processInfo = new SocatProcessInfo
            {
                ProcessId = process.Id,
                TcpHost = string.IsNullOrEmpty(configuration.TcpHost) ? "127.0.0.1" : configuration.TcpHost,
                TcpPort = configuration.TcpPort,
                SerialDevice = serialDevice,
                Configuration = configuration.Clone(),
                Profile = profile?.Clone(),
                CommandLine = $"{fileName} {arguments}",
                StartTime = _timeProvider.GetLocalNow(),
                IsRunning = true,
                Status = SocatProcessStatus.Running,
                ActiveConnections = 0,
                TransferStats = new SocatTransferStats
                {
                    BytesSerialToTcp = 0,
                    BytesTcpToSerial = 0,
                    TotalConnections = 0,
                    ActiveConnections = 0,
                    LastUpdated = _timeProvider.GetLocalNow(),
                    Uptime = TimeSpan.Zero
                },
                LastUpdated = _timeProvider.GetLocalNow()
            };

            // Store process reference
            _activeProcesses[process.Id] = process;

            _logger.LogDebug("Started socat process {ProcessId}", process.Id);
            return processInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start socat process with command: {Command}", command);
            throw new ConnectionException(
                $"{configuration.TcpHost}:{configuration.TcpPort}",
                "Socat",
                $"Failed to start socat process: {ex.Message}",
                ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Stops a socat process gracefully (SIGTERM) then forcefully (SIGKILL).
    /// </summary>
    public async Task<bool> StopProcessAsync(
        int processId,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Process? processToDispose = null;
            Process? process = null;

            if (_activeProcesses.TryGetValue(processId, out Process? storedProcess))
            {
                process = storedProcess;
            }
            else
            {
                // Fallback to system process lookup
                try
                {
                    process = processToDispose = Process.GetProcessById(processId);
                }
                catch (ArgumentException)
                {
                    _logger.LogDebug("Socat process {ProcessId} was already stopped", processId);
                    return true;
                }
            }

            if (process.HasExited)
            {
                CleanupProcess(processId);
                return true;
            }

            // Get child processes before killing parent
            List<int> childPids = await _shellExecutor.GetChildProcessesAsync(processId, cancellationToken).ConfigureAwait(false);
            if (childPids.Count > 0)
            {
                _logger.LogDebug("Found {Count} child processes for socat {ProcessId}: {Children}",
                    childPids.Count, processId, string.Join(", ", childPids));
            }

            bool exited = false;

            // Try SIGTERM first on Unix-like systems
            if (!process.HasExited)
            {
                try
                {
                    if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                    {
                        await _shellExecutor.ExecuteCommandAsync($"kill -TERM {processId}", cancellationToken).ConfigureAwait(false);
                        exited = await WaitForProcessExitAsync(process, timeoutMs / 2, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // On Windows, try CloseMainWindow
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            process.CloseMainWindow();
                            exited = await WaitForProcessExitAsync(process, timeoutMs / 2, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch
                {
                    // Ignore and escalate below
                }
            }

            if (!exited && !process.HasExited)
            {
                _logger.LogWarning("Socat process {ProcessId} did not exit after SIGTERM, forcing termination", processId);
                process.Kill();
                await WaitForProcessExitAsync(process, timeoutMs / 2, cancellationToken).ConfigureAwait(false);
            }

            // Clean up child processes
            if (childPids.Count > 0 && (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()))
            {
                foreach (int childPid in childPids)
                {
                    try
                    {
                        // Check if child is still alive
                        var result = await _shellExecutor.ExecuteCommandAsync($"kill -0 {childPid}", cancellationToken).ConfigureAwait(false);
                        if (result.Success)
                        {
                            _logger.LogInformation("Cleaning up child process {ChildPid} for socat {ProcessId}", childPid, processId);
                            await _shellExecutor.ExecuteCommandAsync($"kill -9 {childPid}", cancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup child process {ChildPid}", childPid);
                    }
                }
            }

            // Dispose fallback process if created
            processToDispose?.Dispose();

            CleanupProcess(processId);
            _logger.LogInformation("Stopped socat process {ProcessId}", processId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop socat process {ProcessId}", processId);
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets process by ID from managed processes.
    /// </summary>
    public async Task<Process?> GetProcessByIdAsync(int processId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _activeProcesses.TryGetValue(processId, out Process? process) ? process : null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Updates process status (checks if still running).
    /// </summary>
    public async Task UpdateProcessStatusAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processInfo);

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_activeProcesses.TryGetValue(processInfo.ProcessId, out Process? process))
            {
                processInfo.IsRunning = !process.HasExited;
                processInfo.Status = process.HasExited ? SocatProcessStatus.Stopped : SocatProcessStatus.Running;
            }
            else
            {
                processInfo.IsRunning = false;
                processInfo.Status = SocatProcessStatus.Unknown;
            }

            processInfo.LastUpdated = _timeProvider.GetLocalNow();

            // Update uptime if still running
            if (processInfo.TransferStats != null)
            {
                processInfo.TransferStats.Uptime = _timeProvider.GetLocalNow() - processInfo.StartTime;
                processInfo.TransferStats.LastUpdated = _timeProvider.GetLocalNow();
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CleanupProcess(int processId)
    {
        if (_activeProcesses.TryGetValue(processId, out Process? process))
        {
            _activeProcesses.Remove(processId);
            try
            {
                process.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing process {ProcessId}", processId);
            }
        }

        if (_processMonitors.TryGetValue(processId, out Timer? monitor))
        {
            _processMonitors.Remove(processId);
            monitor.Dispose();
        }
    }

    private async Task<bool> WaitForProcessExitAsync(Process process, int timeoutMs, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeoutMs);

        try
        {
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Regex pattern for detecting hex dump lines.
    /// </summary>
    [GeneratedRegex(@"^\s+([0-9a-fA-F]{2}\s+)+")]
    private static partial Regex HexDumpRegex();

    /// <summary>
    /// Regex pattern for socat log timestamps.
    /// </summary>
    [GeneratedRegex(@"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} ")]
    private static partial Regex SocatLogTimestampRegex();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose all active processes
            foreach (var process in _activeProcesses.Values)
            {
                try
                { process.Dispose(); }
                catch { }
            }

            // Dispose all monitors
            foreach (var monitor in _processMonitors.Values)
            {
                try
                { monitor.Dispose(); }
                catch { }
            }

            _semaphore.Dispose();
        }

        _disposed = true;
    }
}

/// <summary>
/// Event args for process exit.
/// </summary>
public class ProcessExitedEventArgs : EventArgs
{
    public int ProcessId { get; }
    public int ExitCode { get; }

    public ProcessExitedEventArgs(int processId, int exitCode)
    {
        ProcessId = processId;
        ExitCode = exitCode;
    }
}
