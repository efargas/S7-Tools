using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Socat;

/// <summary>
/// Manages TCP port checking and network connections for socat.
/// Single Responsibility: Port and connection management.
/// </summary>
public class SocatPortManager
{
    private readonly ILogger<SocatPortManager> _logger;

    public SocatPortManager(ILogger<SocatPortManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a TCP port is currently in use by attempting to bind to it.
    /// </summary>
    public async Task<bool> IsPortInUseAsync(int port, CancellationToken cancellationToken = default)
    {
        if (!NetworkConstants.IsValidPort(port))
        {
            throw new ArgumentException($"TCP port must be between {NetworkConstants.MinPort} and {NetworkConstants.MaxPort}", nameof(port));
        }

        try
        {
            await Task.CompletedTask; // For async signature consistency

            // Attempt to bind to the port to detect usage
            try
            {
                using var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();

                _logger.LogDebug("Port {Port} is available (bind test successful)", port);
                return false; // Successfully bound → port not in use
            }
            catch (SocketException ex)
            {
                _logger.LogDebug("Port {Port} is in use (bind failed: {Error})", port, ex.Message);
                return true; // Bind failed → port in use or insufficient privileges
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if TCP port {Port} is in use", port);
            // Be conservative: assume port is in use on error to avoid collisions
            return true;
        }
    }

    /// <summary>
    /// Checks if port is in use by a managed socat process.
    /// </summary>
    public bool IsPortUsedByManagedProcess(int port, IEnumerable<SocatProcessInfo> runningProcesses)
    {
        ArgumentNullException.ThrowIfNull(runningProcesses, nameof(runningProcesses));

        SocatProcessInfo? managedProcess = runningProcesses.FirstOrDefault(p => p.TcpPort == port && p.IsRunning);
        if (managedProcess != null)
        {
            _logger.LogDebug("Port {Port} is in use by managed socat process {ProcessId}",
                port, managedProcess.ProcessId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the socat process that is using a specific TCP port.
    /// </summary>
    public SocatProcessInfo? GetProcessByPort(int port, IEnumerable<SocatProcessInfo> runningProcesses)
    {
        ArgumentNullException.ThrowIfNull(runningProcesses, nameof(runningProcesses));

        if (!NetworkConstants.IsValidPort(port))
        {
            throw new ArgumentException($"TCP port must be between {NetworkConstants.MinPort} and {NetworkConstants.MaxPort}", nameof(port));
        }

        return runningProcesses.FirstOrDefault(p => p.TcpPort == port && p.IsRunning);
    }

    /// <summary>
    /// Tests TCP connection to a host:port.
    /// </summary>
    public async Task<bool> TestConnectionAsync(
        string host,
        int port,
        int timeoutMs = 5000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("Host cannot be null or empty", nameof(host));
        }

        if (!NetworkConstants.IsValidPort(port))
        {
            throw new ArgumentException($"TCP port must be between {NetworkConstants.MinPort} and {NetworkConstants.MaxPort}", nameof(port));
        }

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);

            _logger.LogDebug("Successfully connected to {Host}:{Port}", host, port);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Connection to {Host}:{Port} timed out after {Timeout}ms", host, port, timeoutMs);
            return false;
        }
        catch (SocketException ex)
        {
            _logger.LogWarning("Connection to {Host}:{Port} failed: {Error}", host, port, ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing connection to {Host}:{Port}", host, port);
            return false;
        }
    }

    /// <summary>
    /// Discovers socat processes using system commands (pgrep).
    /// </summary>
    public async Task<List<int>> DiscoverSocatProcessIdsAsync(CancellationToken cancellationToken = default)
    {
        var processIds = new List<int>();

        try
        {
            // Use pgrep to find socat processes safely
            var startInfo = new ProcessStartInfo
            {
                FileName = "pgrep",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("socat");

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                string output = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

                foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(line.Trim(), out int pid))
                    {
                        processIds.Add(pid);
                    }
                }

                if (processIds.Count > 0)
                {
                    _logger.LogDebug("Discovered {Count} socat processes", processIds.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover socat processes using pgrep");
        }

        return processIds;
    }
}
