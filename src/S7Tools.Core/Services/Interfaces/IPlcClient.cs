// IPlcClient.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPlcClient : IAsyncDisposable {
    Task HandshakeAsync(CancellationToken ct = default);
    Task<string> GetBootloaderVersionAsync(CancellationToken ct = default);
    Task InstallStagerAsync(byte[] stager, CancellationToken ct = default);
    Task<byte[]> DumpMemoryAsync(uint address, uint length, byte[] dumpPayload, IProgress<long> progress, CancellationToken ct = default);
}