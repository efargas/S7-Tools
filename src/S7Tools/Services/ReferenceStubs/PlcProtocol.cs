// PlcProtocol.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiemensS7Bootloader.S7.Net
{
    public class PlcProtocol
    {
        public event Action? DataAvailable;
        public Task SendPacketAsync(byte[] payload, int? expectedResponseLength = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<byte[]> ReceivePacketAsync(CancellationToken cancellationToken) => Task.FromResult(Array.Empty<byte>());
        public Task RawWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<int> RawReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(0);
    }
}