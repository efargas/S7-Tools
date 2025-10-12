// ISocatService.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces
{
    public interface ISocatService
    {
        Task<int> EnsureBridgeAsync(string serialDevice, int baud, string tcpHost, int tcpPort, CancellationToken ct = default);
        Task StopBridgeAsync(int tcpPort, CancellationToken ct = default);
        string GenerateSocatCommand(SocatConfiguration configuration, string serialDevice);
        string GenerateSocatCommandForProfile(SocatProfile profile, string serialDevice);
        Task<SocatProcessInfo> StartSocatAsync(SocatConfiguration configuration, string serialDevice, CancellationToken cancellationToken = default);
        Task<SocatProcessInfo> StartSocatWithProfileAsync(SocatProfile profile, string serialDevice, CancellationToken cancellationToken = default);
        Task<bool> StopSocatAsync(SocatProcessInfo processInfo, CancellationToken cancellationToken = default);
        Task<int> StopAllSocatProcessesAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<SocatProcessInfo>> GetRunningProcessesAsync(CancellationToken cancellationToken = default);
        Task<bool> TestTcpConnectionAsync(string tcpHost, int tcpPort, int timeoutMs = 5000, CancellationToken cancellationToken = default);
        SocatCommandValidationResult ValidateSocatCommand(string command);
        Task<bool> IsPortInUseAsync(int tcpPort, CancellationToken cancellationToken = default);
        Task<SocatProcessInfo?> GetProcessByPortAsync(int tcpPort, CancellationToken cancellationToken = default);

        event EventHandler<SocatProcessEventArgs>? ProcessStarted;
        event EventHandler<SocatProcessEventArgs>? ProcessStopped;
        event EventHandler<SocatProcessErrorEventArgs>? ProcessError;
        event EventHandler<SocatConnectionEventArgs>? ConnectionEstablished;
        event EventHandler<SocatConnectionEventArgs>? ConnectionClosed;
    }
}