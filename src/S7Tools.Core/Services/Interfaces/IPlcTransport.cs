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