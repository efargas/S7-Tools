// PayloadProviderAdapter.cs
namespace S7Tools.Services.Adapters;

using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.ReferenceStubs;
using System.Threading;
using System.Threading.Tasks;
public sealed class PayloadProviderAdapter : IPayloadProvider {
    private readonly PayloadManager _inner;
    public PayloadProviderAdapter(string baseDir) { _inner = new PayloadManager(baseDir); }
    public Task<byte[]> GetStagerAsync(string basePath, CancellationToken ct = default) => _inner.GetStagerPayloadAsync(basePath);
    public Task<byte[]> GetMemoryDumperAsync(string basePath, CancellationToken ct = default) => _inner.GetMemoryDumperPayloadAsync(basePath);
}