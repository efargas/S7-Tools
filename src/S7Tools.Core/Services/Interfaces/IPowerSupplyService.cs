// IPowerSupplyService.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPowerSupplyService {
    Task PowerCycleAsync(string host, int port, int coil, int delaySeconds, CancellationToken ct = default);
    Task SetPowerAsync(string host, int port, int coil, bool on, CancellationToken ct = default);
}