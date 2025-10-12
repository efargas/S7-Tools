// TcpChannel.cs (Stub)
namespace SiemensS7Bootloader.S7.Net
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class TcpChannel : ITcpChannel, ICommunicationChannel
    {
        public TcpChannel(string host, int port) { }
        public bool IsConnected => true;
        public bool DataAvailable => true;
        public Task ConnectAsync(CancellationToken ct) => Task.CompletedTask;
        public Task DisconnectAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => Task.FromResult(0);
        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) => Task.CompletedTask;
        public void Dispose() { }
    }
}