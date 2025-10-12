// ICommunicationChannel.cs (Stub)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiemensS7Bootloader.S7.Net
{
    public interface ICommunicationChannel : IDisposable
    {
        bool IsConnected { get; }
        bool DataAvailable { get; }
        Task ConnectAsync(CancellationToken ct);
        Task DisconnectAsync(CancellationToken ct);
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct);
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct);
    }
}