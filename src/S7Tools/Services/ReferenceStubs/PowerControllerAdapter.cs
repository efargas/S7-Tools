// PowerControllerAdapter.cs (Stub)
namespace SiemensS7Bootloader.S7.Core.Commands
{
    using System.Threading;
    using System.Threading.Tasks;

    public class PowerControllerAdapter
    {
        public Task PowerCycleAsync(string h, int p, int c, int d, CancellationToken ct = default) => Task.CompletedTask;
    }
}