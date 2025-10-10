<!-- markdownlint-disable-file -->
# Task Details: Siemens S7 Bootloader Integration (DDD + MVVM + Clean Architecture)

## Research Reference

- Reference code: ./.github/agents/workspace/referent-projects/SiemensS7-Bootloader/src/S7_Csharp_Core/S7.Net/* (PlcClient, PlcProtocol, Channels, PayloadManager)
- Project standards: /AGENTS.md, .editorconfig, Memory Bank instructions

---

## Phase 1: Core Contracts & Models

### Task 1.1: Add interfaces in S7Tools.Core/Services/Interfaces

Create the following files under `src/S7Tools.Core/Services/Interfaces/`:

```csharp
// IPlcTransport.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPlcTransport : IAsyncDisposable {
    bool IsConnected { get; }
    bool DataAvailable { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct);
    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct);
}
```

```csharp
// IPlcProtocol.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPlcProtocol {
    bool DataAvailable { get; }
    Task SendPacketAsync(byte[] payload, int? maxChunk = null, CancellationToken ct = default);
    Task<byte[]> ReceivePacketAsync(CancellationToken ct = default);
    Task RawWriteAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);
    Task<int> RawReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);
}
```

```csharp
// IPlcClient.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPlcClient : IAsyncDisposable {
    Task HandshakeAsync(CancellationToken ct = default);
    Task<string> GetBootloaderVersionAsync(CancellationToken ct = default);
    Task InstallStagerAsync(byte[] stager, CancellationToken ct = default);
    Task<byte[]> DumpMemoryAsync(uint address, uint length, byte[] dumpPayload, IProgress<long> progress, CancellationToken ct = default);
}
```

```csharp
// IPayloadProvider.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPayloadProvider {
    Task<byte[]> GetStagerAsync(string basePath, CancellationToken ct = default);
    Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken ct = default);
}
```

```csharp
// IPowerSupplyService.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPowerSupplyService {
    Task PowerCycleAsync(string host, int port, int coil, int delaySeconds, CancellationToken ct = default);
    Task SetPowerAsync(string host, int port, int coil, bool on, CancellationToken ct = default);
}
```

```csharp
// ISocatService.cs
namespace S7Tools.Core.Services.Interfaces;
public interface ISocatService {
    Task<int> EnsureBridgeAsync(string serialDevice, int baud, string tcpHost, int tcpPort, CancellationToken ct = default);
    Task StopBridgeAsync(int tcpPort, CancellationToken ct = default);
}
```

```csharp
// IBootloaderService.cs
namespace S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Models.Jobs;
public interface IBootloaderService {
    Task<byte[]> DumpAsync(JobProfileSet profiles, IProgress<(string stage, double pct)> progress, CancellationToken ct = default);
}
```

```csharp
// IJobScheduler.cs
namespace S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Models.Jobs;
public interface IJobScheduler {
    event Action<JobStateChanged>? JobStateChanged;
    Guid Enqueue(Job job);
    IReadOnlyCollection<Job> GetAll();
}
public delegate void JobStateChanged(Guid jobId, JobState state, string? message);
```

```csharp
// IResourceCoordinator.cs
namespace S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Models.Jobs;
public interface IResourceCoordinator {
    bool TryAcquire(IEnumerable<ResourceKey> keys);
    void Release(IEnumerable<ResourceKey> keys);
}
```

### Task 1.2: Add Job and Profile models in S7Tools.Core/Models

Create the following under `src/S7Tools.Core/Models/Jobs/`:

```csharp
// JobState.cs
namespace S7Tools.Core.Models.Jobs;
public enum JobState { Created, Queued, Running, Completed, Failed, Canceled }
```

```csharp
// ResourceKey.cs
namespace S7Tools.Core.Models.Jobs;
public readonly record struct ResourceKey(string Kind, string Id) {
    public override string ToString() => $"{Kind}:{Id}";
}
```

```csharp
// Job.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record Job(
    Guid Id,
    string Name,
    IReadOnlyList<ResourceKey> Resources,
    JobProfileSet Profiles,
    JobState State,
    DateTimeOffset CreatedAt
);
```

```csharp
// JobProfileSet.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record JobProfileSet(
    SerialProfileRef Serial,
    SocatProfileRef Socat,
    PowerProfileRef Power,
    MemoryRegionProfile Memory,
    PayloadSetProfile Payloads,
    string OutputPath
);
```

Create the following under `src/S7Tools.Core/Models/Profiles/`:

```csharp
// SerialProfileRef.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record SerialProfileRef(string Device, int Baud, string Parity, int DataBits, string StopBits);
```

```csharp
// SocatProfileRef.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record SocatProfileRef(int Port, bool Ephemeral = true);
```

```csharp
// PowerProfileRef.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record PowerProfileRef(string Host, int Port, int Coil, int DelaySeconds);
```

```csharp
// MemoryRegionProfile.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record MemoryRegionProfile(uint Start, uint Length);
```

```csharp
// PayloadSetProfile.cs
namespace S7Tools.Core.Models.Jobs;
public sealed record PayloadSetProfile(string BasePath);
```

---

## Phase 2: Adapters (Reference Wrappers)

### Task 2.1: PlcTransport/Protocol/Client adapters

Create under `src/S7Tools/Services/Adapters/`:

```csharp
// PlcTransportAdapter.cs
using S7Tools.Core.Services.Interfaces;
using Reference = SiemensS7Bootloader.S7.Net; // alias your reference root namespace

public sealed class PlcTransportAdapter : IPlcTransport {
    private readonly Reference.ICommunicationChannel _inner;
    public PlcTransportAdapter(Reference.ICommunicationChannel inner) => _inner = inner;
    public bool IsConnected => _inner.IsConnected;
    public bool DataAvailable => _inner.DataAvailable;
    public Task ConnectAsync(CancellationToken ct) => _inner.ConnectAsync(ct);
    public Task DisconnectAsync(CancellationToken ct) => _inner.DisconnectAsync(ct);
    public Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.ReadAsync(b, o, c, ct);
    public Task WriteAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.WriteAsync(b, o, c, ct);
    public ValueTask DisposeAsync() { (_inner as IDisposable)?.Dispose(); return ValueTask.CompletedTask; }
}
```

```csharp
// PlcProtocolAdapter.cs
using S7Tools.Core.Services.Interfaces;
using Reference = SiemensS7Bootloader.S7.Net;
public sealed class PlcProtocolAdapter : IPlcProtocol {
    private readonly Reference.PlcProtocol _inner;
    public PlcProtocolAdapter(Reference.PlcProtocol inner) => _inner = inner;
    public bool DataAvailable => _inner.DataAvailable;
    public Task SendPacketAsync(byte[] p, int? mc, CancellationToken ct) => _inner.SendPacketAsync(p, cancellationToken: ct);
    public Task<byte[]> ReceivePacketAsync(CancellationToken ct) => _inner.ReceivePacketAsync(ct);
    public Task RawWriteAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.RawWriteAsync(b, o, c, ct);
    public Task<int> RawReadAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.RawReadAsync(b, o, c, ct);
}
```

```csharp
// PlcClientAdapter.cs
using S7Tools.Core.Services.Interfaces;
using Reference = SiemensS7Bootloader.S7.Net;
public sealed class PlcClientAdapter : IPlcClient {
    private readonly Reference.PlcClient _inner;
    public PlcClientAdapter(Reference.PlcClient inner) => _inner = inner;
    public Task HandshakeAsync(CancellationToken ct = default) => _inner.HandshakeAsync(ct);
    public Task<string> GetBootloaderVersionAsync(CancellationToken ct = default) => _inner.GetVersionAsync(ct);
    public Task InstallStagerAsync(byte[] stager, CancellationToken ct = default) => _inner.InstallStager(stager, ct);
    public Task<byte[]> DumpMemoryAsync(uint a, uint l, byte[] d, IProgress<long> p, CancellationToken ct = default) => _inner.DumpMemoryAsync(a, l, d, p, ct);
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
}
```

### Task 2.2: Payload, Power, Socat adapters

```csharp
// PayloadProviderAdapter.cs
using S7Tools.Core.Services.Interfaces;
using Reference = SiemensS7Bootloader.S7.Net;
public sealed class PayloadProviderAdapter : IPayloadProvider {
    private readonly Reference.PayloadManager _inner;
    public PayloadProviderAdapter(string baseDir) { _inner = new Reference.PayloadManager(baseDir); }
    public Task<byte[]> GetStagerAsync(string basePath, CancellationToken ct = default) => _inner.GetStagerPayloadAsync(basePath);
    public Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken ct = default) => _inner.GetMemoryDumperPayloadAsync(basePath);
}
```

```csharp
// PowerSupplyAdapter.cs
using S7Tools.Core.Services.Interfaces;
using Reference = SiemensS7Bootloader.S7.Core.Commands; // PowerControllerAdapter
public sealed class PowerSupplyAdapter : IPowerSupplyService {
    private readonly Reference.PowerControllerAdapter _inner;
    public PowerSupplyAdapter(Reference.PowerControllerAdapter inner) => _inner = inner;
    public Task PowerCycleAsync(string h, int p, int c, int d, CancellationToken ct = default) => _inner.PowerCycleAsync(h, p, c, d, ct);
    public Task SetPowerAsync(string h, int p, int c, bool on, CancellationToken ct = default) => throw new NotImplementedException("Ref adapter supports cycle; extend if needed");
}
```

```csharp
// SocatAdapter.cs
using S7Tools.Core.Services.Interfaces;
public sealed class SocatAdapter : ISocatService {
    private readonly SocatService _inner; // use existing app service if present
    public SocatAdapter(SocatService inner) => _inner = inner;
    public Task<int> EnsureBridgeAsync(string dev, int baud, string host, int port, CancellationToken ct = default) => _inner.EnsureSocatBridgeAsync(dev, baud, host, port, ct);
    public Task StopBridgeAsync(int port, CancellationToken ct = default) => _inner.StopSocatBridgeAsync(port, ct);
}
```

---

## Phase 3: Bootloader Orchestration

### Task 3.1: Implement IBootloaderService and BootloaderService

Create under `src/S7Tools/Services/Bootloader/`:

```csharp
// BootloaderService.cs
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

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
```

---

## Phase 4: Task Manager (Scheduling & UI)

### Task 4.1: ResourceCoordinator and JobScheduler

Create under `src/S7Tools/Services/Tasking/`:

```csharp
// ResourceCoordinator.cs
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
public sealed class ResourceCoordinator : IResourceCoordinator {
    private readonly HashSet<ResourceKey> _locks = new();
    public bool TryAcquire(IEnumerable<ResourceKey> keys) {
        var k = keys.ToArray();
        if (k.Any(_locks.Contains)) return false;
        foreach (var rk in k) _locks.Add(rk);
        return true;
    }
    public void Release(IEnumerable<ResourceKey> keys) { foreach (var rk in keys) _locks.Remove(rk); }
}
```

```csharp
// JobEvents.cs
using S7Tools.Core.Models.Jobs;
namespace S7Tools.Services.Tasking;
public sealed record JobProgressEvent(Guid JobId, string Stage, double Percent);
```

```csharp
// JobScheduler.cs
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using System.Collections.Concurrent;

public sealed class JobScheduler : IJobScheduler {
    private readonly ILogger<JobScheduler> _logger;
    private readonly IResourceCoordinator _resources;
    private readonly IBootloaderService _bootloader;
    private readonly ConcurrentQueue<Job> _queue = new();
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

    public event JobStateChanged? JobStateChanged;

    public JobScheduler(ILogger<JobScheduler> logger, IResourceCoordinator resources, IBootloaderService bootloader) {
        _logger = logger; _resources = resources; _bootloader = bootloader;
    }

    public Guid Enqueue(Job job) {
        _jobs[job.Id] = job with { State = JobState.Queued };
        _queue.Enqueue(job);
        JobStateChanged?.Invoke(job.Id, JobState.Queued, null);
        _ = TryStartNextAsync();
        return job.Id;
    }

    public IReadOnlyCollection<Job> GetAll() => _jobs.Values.ToList();

    private async Task TryStartNextAsync() {
        if (!_queue.TryDequeue(out var job)) return;
        if (!_resources.TryAcquire(job.Resources)) { _queue.Enqueue(job); return; }
        _jobs[job.Id] = job with { State = JobState.Running };
        JobStateChanged?.Invoke(job.Id, JobState.Running, null);
        try {
            var progress = new Progress<(string stage, double pct)>(p => JobStateChanged?.Invoke(job.Id, JobState.Running, $"{p.stage}:{p.pct:P0}"));
            var bytes = await _bootloader.DumpAsync(job.Profiles, progress, CancellationToken.None).ConfigureAwait(false);
            var outFile = Path.Combine(job.Profiles.OutputPath, $"dump-{job.Id}.bin");
            Directory.CreateDirectory(job.Profiles.OutputPath);
            await File.WriteAllBytesAsync(outFile, bytes).ConfigureAwait(false);
            _jobs[job.Id] = job with { State = JobState.Completed };
            JobStateChanged?.Invoke(job.Id, JobState.Completed, outFile);
        } catch (Exception ex) {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);
            _jobs[job.Id] = job with { State = JobState.Failed };
            JobStateChanged?.Invoke(job.Id, JobState.Failed, ex.Message);
        } finally {
            _resources.Release(job.Resources);
            _ = TryStartNextAsync();
        }
    }
}
```

### Task 4.2: Task Manager ViewModels and Views

Sketch for VM assembly under `src/S7Tools/ViewModels/TaskManager/`:

```csharp
// TaskManagerViewModel.cs
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
public sealed class TaskManagerViewModel : ViewModelBase {
    private readonly IJobScheduler _scheduler;
    public ObservableCollection<Job> Jobs { get; } = new();
    public ReactiveCommand<Unit, Unit> CreateJobCommand { get; }
    public TaskManagerViewModel(IJobScheduler scheduler) {
        _scheduler = scheduler;
        _scheduler.JobStateChanged += (id, state, msg) => {
            var idx = Jobs.Select((j,i)=>(j,i)).FirstOrDefault(t => t.j.Id==id).i;
            if (idx>=0) Jobs[idx] = Jobs[idx] with { State = state };
        };
        CreateJobCommand = ReactiveCommand.Create(EnqueueSampleJob);
    }
    private void EnqueueSampleJob() {
        var profiles = new JobProfileSet(
            new SerialProfileRef("/dev/ttyUSB0", 115200, "None", 8, "One"),
            new SocatProfileRef(20000, true),
            new PowerProfileRef("127.0.0.1", 502, 1, 3),
            new MemoryRegionProfile(0x00000000, 0x00010000),
            new PayloadSetProfile("resources/payloads"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dumps"));
        var resources = new []{
            new ResourceKey("serial","/dev/ttyUSB0"),
            new ResourceKey("tcp","127.0.0.1:20000"),
            new ResourceKey("power","127.0.0.1:502:1")};
        var job = new Job(Guid.NewGuid(), "Dump S7-1200", resources, profiles, JobState.Created, DateTimeOffset.Now);
        Jobs.Add(job);
        _scheduler.Enqueue(job);
    }
}
```

XAML views: list of jobs with state, create button. Bind to CreateJobCommand for demo; later replace with full job creation dialog.

---

## Phase 5: DI, Tests, and Verification

### Task 5.1: Register services in ServiceCollectionExtensions.cs

```csharp
// ServiceCollectionExtensions.cs additions
services.AddSingleton<IResourceCoordinator, ResourceCoordinator>();
services.AddSingleton<IJobScheduler, JobScheduler>();
services.AddTransient<IBootloaderService, BootloaderService>();
services.AddTransient<IPayloadProvider>(sp => new PayloadProviderAdapter(AppContext.BaseDirectory));
services.AddTransient<ISocatService, SocatAdapter>();
services.AddTransient<IPowerSupplyService, PowerSupplyAdapter>();
services.AddTransient<Func<JobProfileSet, IPlcClient>>(sp => profiles => {
    // Create reference channel based on profile (serial+socat -> TCP channel)
    // var channel = new Reference.TcpChannel("127.0.0.1", profiles.Socat.Port);
    // var client = new Reference.PlcClient(channel, sp.GetRequiredService<ILoggerFactory>());
    // return new PlcClientAdapter(client);
    throw new NotImplementedException("Wire reference channel + client here");
});
```

### Task 5.2: Add unit/integration tests

- `tests/S7Tools.Core.Tests/Tasking/ResourceCoordinatorTests.cs`
- `tests/S7Tools.Core.Tests/Tasking/SchedulerTests.cs`
- `tests/S7Tools.Tests/Services/Bootloader/BootloaderServiceTests.cs`
- Fake implementations for IPayloadProvider, IPowerSupplyService, ISocatService, IPlcClient

Example:

```csharp
[Fact]
public async Task Scheduler_runs_jobs_with_disjoint_resources_in_parallel() {
    // Arrange fakes and two jobs with different resources
    // Assert both complete without waiting for the other
}
```

---

## Performance Considerations

- Stream dumps to file to avoid large allocations; optionally return path instead of bytes
- ConfigureAwait(false) in services; cancellation respected; retry policies on handshake/power

## Security Requirements

- Sanitize paths in logs; avoid logging payload bytes; no secrets in configuration

## Testing Strategy

- Unit for concurrency, orchestration; integration with reference fakes; manual S7-1200 runbook

## Deployment Considerations

- Cross-platform serial dependencies; socat availability; ensure process cleanup on exit
