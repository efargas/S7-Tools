// PowerSupplyAdapter.cs
namespace S7Tools.Services;

using S7Tools.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using SiemensS7Bootloader.S7.Core.Commands;

public sealed class PowerSupplyAdapter : IPowerSupplyService {
    private readonly PowerControllerAdapter _inner;
    public PowerSupplyAdapter(PowerControllerAdapter inner) => _inner = inner;
    public Task PowerCycleAsync(string h, int p, int c, int d, CancellationToken ct = default) => _inner.PowerCycleAsync(h, p, c, d, ct);
    public Task SetPowerAsync(string h, int p, int c, bool on, CancellationToken ct = default) => _inner.SetPowerAsync(h, p, c, on, ct);
}