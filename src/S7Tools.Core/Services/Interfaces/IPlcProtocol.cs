// IPlcProtocol.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPlcProtocol {
    Task SendPacketAsync(byte[] payload, int? maxChunk = null, CancellationToken ct = default);
    Task<byte[]> ReceivePacketAsync(CancellationToken ct = default);
    Task RawWriteAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);
    Task<int> RawReadAsync(byte[] buffer, int offset, int count, CancellationToken ct = default);
}