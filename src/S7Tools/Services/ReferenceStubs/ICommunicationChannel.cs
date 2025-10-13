// ICommunicationChannel.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiemensS7Bootloader.S7.Net
{
    public interface ICommunicationChannel : IAsyncDisposable
    {
        bool IsConnected { get; }
        bool DataAvailable { get; }
        Task ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}