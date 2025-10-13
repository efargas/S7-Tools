// PlcClientAdapter.cs
namespace S7Tools.Services;

using S7Tools.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using S7.Net;

public sealed class PlcClientAdapter : IPlcClient
{
    private readonly Plc _inner;

    public PlcClientAdapter(Plc inner) => _inner = inner;

    public async Task HandshakeAsync(CancellationToken ct = default)
    {
        await _inner.OpenAsync();
    }

    public Task<string> GetBootloaderVersionAsync(CancellationToken ct = default)
    {
        // S7.Net.Plc does not have a direct equivalent, returning a placeholder.
        return Task.FromResult("S7.Net.Plc");
    }

    public async Task InstallStagerAsync(byte[] stager, CancellationToken ct = default)
    {
        // This is a complex operation that would require a custom implementation.
        // For now, we'll simulate a successful write.
        await Task.CompletedTask;
    }

    public async Task<byte[]> DumpMemoryAsync(uint a, uint l, byte[] d, IProgress<long> p, CancellationToken ct = default)
    {
        var buffer = new byte[l];
        // S7.Net.Plc does not have a direct equivalent for this custom bootloader function.
        // We will simulate a read operation.
        // This would need to be replaced with the actual implementation.
        await _inner.ReadBytesAsync(DataType.DataBlock, 1, (int)a, (int)l, ct);
        return buffer;
    }

    public ValueTask DisposeAsync()
    {
        _inner.Close();
        return ValueTask.CompletedTask;
    }
}