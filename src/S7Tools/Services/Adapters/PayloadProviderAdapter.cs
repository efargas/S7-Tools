// PayloadProviderAdapter.cs
namespace S7Tools.Services;

using S7Tools.Core.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;
using SiemensS7Bootloader.S7.Core;
using SiemensS7Bootloader.S7.Net;

public sealed class PayloadProviderAdapter : IPayloadProvider {
    private readonly PayloadManager _inner;
    public PayloadProviderAdapter(string baseDir) { _inner = new PayloadManager(baseDir); }
    public Task<byte[]> GetStagerAsync(string basePath, CancellationToken ct = default) => _inner.GetStagerPayloadAsync(basePath);
    public Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken ct = default) => _inner.GetMemoryDumperPayloadAsync(basePath);
}