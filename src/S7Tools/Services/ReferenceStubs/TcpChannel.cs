// TcpChannel.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiemensS7Bootloader.S7.Net
{
    public class TcpChannel : ITcpChannel
    {
        public TcpChannel(string host, int port)
        {
        }
        public bool IsConnected => true;
        public bool DataAvailable => true;
        public Task ConnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}