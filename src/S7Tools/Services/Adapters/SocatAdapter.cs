// SocatAdapter.cs
namespace S7Tools.Services.Adapters;

using S7Tools.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Models;

public sealed class SocatAdapter : ISocatService
{
    private readonly SocatService _inner; // use existing app service
    public SocatAdapter(SocatService inner)
    {
        _inner = inner;
        _inner.ProcessStarted += (s, e) => ProcessStarted?.Invoke(s, e);
        _inner.ProcessStopped += (s, e) => ProcessStopped?.Invoke(s, e);
        _inner.ProcessError += (s, e) => ProcessError?.Invoke(s, e);
        _inner.ConnectionEstablished += (s, e) => ConnectionEstablished?.Invoke(s, e);
        _inner.ConnectionClosed += (s, e) => ConnectionClosed?.Invoke(s, e);
    }

    public event EventHandler<SocatProcessEventArgs>? ProcessStarted;
    public event EventHandler<SocatProcessEventArgs>? ProcessStopped;
    public event EventHandler<SocatProcessErrorEventArgs>? ProcessError;
    public event EventHandler<SocatConnectionEventArgs>? ConnectionEstablished;
    public event EventHandler<SocatConnectionEventArgs>? ConnectionClosed;

    public Task<int> EnsureBridgeAsync(string serialDevice, int baud, string tcpHost, int tcpPort, CancellationToken ct = default) => ((ISocatService)_inner).EnsureBridgeAsync(serialDevice, baud, tcpHost, tcpPort, ct);
    public Task StopBridgeAsync(int tcpPort, CancellationToken ct = default) => ((ISocatService)_inner).StopBridgeAsync(tcpPort, ct);
    public string GenerateSocatCommand(SocatConfiguration configuration, string serialDevice) => _inner.GenerateSocatCommand(configuration, serialDevice);
    public string GenerateSocatCommandForProfile(SocatProfile profile, string serialDevice) => _inner.GenerateSocatCommandForProfile(profile, serialDevice);
    public Task<SocatProcessInfo> StartSocatAsync(SocatConfiguration configuration, string serialDevice, CancellationToken cancellationToken = default) => _inner.StartSocatAsync(configuration, serialDevice, cancellationToken);
    public Task<SocatProcessInfo> StartSocatWithProfileAsync(SocatProfile profile, string serialDevice, CancellationToken cancellationToken = default) => _inner.StartSocatWithProfileAsync(profile, serialDevice, cancellationToken);
    public Task<bool> StopSocatAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken = default) => _inner.StopSocatAsync(processInfo, cancellationToken);
    public Task<int> StopAllSocatProcessesAsync(CancellationToken cancellationToken = default) => _inner.StopAllSocatProcessesAsync(cancellationToken);
    public Task<IEnumerable<SocatProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default) => _inner.GetRunningProcessesAsync(cancellationToken);
    public Task<bool> TestTcpConnectionAsync(string tcpHost, int tcpPort, int timeoutMs = 5000, CancellationToken cancellationToken = default) => _inner.TestTcpConnectionAsync(tcpHost, tcpPort, timeoutMs, cancellationToken);
    public SocatCommandValidationResult ValidateSocatCommand(string command) => _inner.ValidateSocatCommand(command);
    public Task<bool> IsPortInUseAsync(int tcpPort, CancellationToken cancellationToken = default) => _inner.IsPortInUseAsync(tcpPort, cancellationToken);
    public Task<SocatProcessInfo?> GetProcessByPortAsync(int tcpPort, CancellationToken cancellationToken = default) => _inner.GetProcessByPortAsync(tcpPort, cancellationToken);
}