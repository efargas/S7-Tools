// PlcProtocolAdapter.cs
namespace S7Tools.Services;

using S7Tools.Core.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using SiemensS7Bootloader.S7.Net;

public sealed class PlcProtocolAdapter : IPlcProtocol {
    private readonly PlcProtocol _inner;
    public PlcProtocolAdapter(PlcProtocol inner) => _inner = inner;
    public bool DataAvailable => _inner.DataAvailable;
    public Task SendPacketAsync(byte[] p, int? mc, CancellationToken ct) => _inner.SendPacketAsync(p, cancellationToken: ct);
    public Task<byte[]> ReceivePacketAsync(CancellationToken ct) => _inner.ReceivePacketAsync(ct);
    public Task RawWriteAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.RawWriteAsync(b, o, c, ct);
    public Task<int> RawReadAsync(byte[] b, int o, int c, CancellationToken ct) => _inner.RawReadAsync(b, o, c, ct);
}