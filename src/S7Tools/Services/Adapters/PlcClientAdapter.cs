// PlcClientAdapter.cs
namespace S7Tools.Services;

using S7Tools.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using SiemensS7Bootloader.S7.Net;

public sealed class PlcClientAdapter : IPlcClient {
    private readonly PlcClient _inner;
    public PlcClientAdapter(PlcClient inner) => _inner = inner;
    public Task HandshakeAsync(CancellationToken ct = default) => _inner.HandshakeAsync(ct);
    public Task<string> GetBootloaderVersionAsync(CancellationToken ct = default) => _inner.GetVersionAsync(ct);
    public Task InstallStagerAsync(byte[] stager, CancellationToken ct = default) => _inner.InstallStager(stager, ct);
    public Task<byte[]> DumpMemoryAsync(uint a, uint l, byte[] d, IProgress<long> p, CancellationToken ct = default) => _inner.DumpMemoryAsync(a, l, d, p, ct);
    public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
}