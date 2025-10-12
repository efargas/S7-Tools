// IResourceCoordinator.cs
namespace S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Models.Jobs;
public interface IResourceCoordinator {
    bool TryAcquire(IEnumerable<ResourceKey> keys);
    void Release(IEnumerable<ResourceKey> keys);
}