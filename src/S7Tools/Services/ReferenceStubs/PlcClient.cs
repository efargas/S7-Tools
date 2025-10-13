// PlcClient.cs
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.Profiles;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiemensS7Bootloader.S7.Net
{
    public class PlcClient : IDisposable
    {
        public PlcClient(ITcpChannel channel, ILoggerFactory loggerFactory)
        {
        }

        public void Dispose()
        {
        }

        public Task HandshakeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<string> GetVersionAsync(CancellationToken cancellationToken) => Task.FromResult("1.0.0");
        public Task InstallStager(byte[] stager, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<byte[]> DumpMemoryAsync(uint start, uint length, byte[] dumper, IProgress<long> progress, CancellationToken cancellationToken) => Task.FromResult(Array.Empty<byte>());
    }
}