// ResourceCoordinator.cs
namespace S7Tools.Services.Tasking;

using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;

public sealed class ResourceCoordinator : IResourceCoordinator {
    private readonly HashSet<ResourceKey> _locks = new();
    public bool TryAcquire(IEnumerable<ResourceKey> keys) {
        var k = keys.ToArray();
        lock (_locks)
        {
            if (k.Any(_locks.Contains)) return false;
            foreach (var rk in k) _locks.Add(rk);
            return true;
        }
    }
    public void Release(IEnumerable<ResourceKey> keys) {
        lock (_locks)
        {
            foreach (var rk in keys) _locks.Remove(rk);
        }
    }
}