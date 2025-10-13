// PowerControllerAdapter.cs
using System.Threading;
using System.Threading.Tasks;

namespace S7Tools.Services.ReferenceStubs
{
    public class PowerControllerAdapter
    {
        public Task PowerCycleAsync(string h, int p, int c, int d, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetPowerAsync(string h, int p, int c, bool on, CancellationToken ct = default) => Task.CompletedTask;
    }
}