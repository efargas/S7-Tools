// BootloaderService.cs
namespace S7Tools.Services;

using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public sealed class BootloaderService : IBootloaderService {
    private readonly ILogger<BootloaderService> _logger;
    private readonly IPayloadProvider _payloads;
    private readonly ISocatService _socat;
    private readonly IPowerSupplyService _power;
    private readonly Func<JobProfileSet, IPlcClient> _clientFactory;
    public BootloaderService(ILogger<BootloaderService> logger, IPayloadProvider payloads, ISocatService socat, IPowerSupplyService power, Func<JobProfileSet, IPlcClient> clientFactory) {
        _logger = logger; _payloads = payloads; _socat = socat; _power = power; _clientFactory = clientFactory;
    }
    public async Task<byte[]> DumpAsync(JobProfileSet p, IProgress<(string stage, double pct)> prog, CancellationToken ct = default) {
        prog.Report(("socat", 0.05));
        await _socat.EnsureBridgeAsync(p.Serial.Device, p.Serial.Baud, "127.0.0.1", p.Socat.Port, ct).ConfigureAwait(false);
        prog.Report(("power", 0.10));
        await _power.PowerCycleAsync(p.Power.Host, p.Power.Port, p.Power.Coil, p.Power.DelaySeconds, ct).ConfigureAwait(false);
        await using var client = _clientFactory(p);
        prog.Report(("handshake", 0.20));
        await client.HandshakeAsync(ct).ConfigureAwait(false);
        prog.Report(("stager", 0.30));
        var stager = await _payloads.GetStagerAsync(p.Payloads.BasePath, ct).ConfigureAwait(false);
        await client.InstallStagerAsync(stager, ct).ConfigureAwait(false);
        prog.Report(("dump", 0.50));
        var dumper = await _payloads.GetMemoryDumperAsync(p.Payloads.BasePath, ct).ConfigureAwait(false);
        var bytes = await client.DumpMemoryAsync(p.Memory.Start, p.Memory.Length, dumper, new Progress<long>(_ => { }), ct).ConfigureAwait(false);
        prog.Report(("teardown", 0.95));
        return bytes;
    }
}