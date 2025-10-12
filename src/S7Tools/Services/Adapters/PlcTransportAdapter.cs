// PlcTransportAdapter.cs
namespace S7Tools.Services;
using S7Tools.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using SiemensS7Bootloader.S7.Net;

public sealed class PlcTransportAdapter : IPlcTransport {
    private readonly ICommunicationChannel _inner;
    public PlcTransportAdapter(ICommunicationChannel inner) => _inner = inner;
    public bool IsConnected => _inner.IsConnected;
    public bool DataAvailable => _inner.DataAvailable;
    public Task ConnectAsync(CancellationToken ct) => _inner.ConnectAsync(ct);
    public Task DisconnectAsync(CancellationToken ct) => _inner.DisconnectAsync(ct);
    public Task<int> ReadAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.ReadAsync(b, o, c, ct);
    public Task WriteAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.WriteAsync(b, o, c, ct);
    public ValueTask DisposeAsync() { (_inner as IDisposable)?.Dispose(); return ValueTask.CompletedTask; }
}