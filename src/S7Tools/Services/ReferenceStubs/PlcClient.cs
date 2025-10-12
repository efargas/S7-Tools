// PlcClient.cs (Stub)
using Microsoft.Extensions.Logging;

namespace SiemensS7Bootloader.S7.Net
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class PlcClient : IDisposable
    {
        public PlcClient(ITcpChannel channel, ILoggerFactory loggerFactory)
        {
        }

        public void Dispose() { }
        public Task HandshakeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<string> GetVersionAsync(CancellationToken ct = default) => Task.FromResult("1.0.0");
        public Task InstallStager(byte[] stager, CancellationToken ct = default) => Task.CompletedTask;
        public Task<byte[]> DumpMemoryAsync(uint a, uint l, byte[] d, IProgress<long> p, CancellationToken ct = default) => Task.FromResult(new byte[0]);
    }
}