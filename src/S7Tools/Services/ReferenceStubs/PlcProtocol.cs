// PlcProtocol.cs (Stub)
namespace SiemensS7Bootloader.S7.Net
{
    using System.Threading;
    using System.Threading.Tasks;

    public class PlcProtocol
    {
        public bool DataAvailable => false;
        public Task SendPacketAsync(byte[] p, int? mc = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<byte[]> ReceivePacketAsync(CancellationToken ct = default) => Task.FromResult(new byte[0]);
        public Task RawWriteAsync(byte[] b, int o, int c, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> RawReadAsync(byte[] b, int o, int c, CancellationToken ct = default) => Task.FromResult(0);
    }
}