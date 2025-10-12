// IPayloadProvider.cs
namespace S7Tools.Core.Services.Interfaces;
public interface IPayloadProvider {
    Task<byte[]> GetStagerAsync(string basePath, CancellationToken ct = default);
    Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken ct = default);
}