// SimulatedPlcClient.cs
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiemensS7Bootloader.S7.Net
{
    public class SimulatedPlcClient : IPlcClient
    {
        private readonly ILogger<SimulatedPlcClient> _logger;

        public SimulatedPlcClient(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SimulatedPlcClient>();
        }

        public ValueTask DisposeAsync()
        {
            _logger.LogInformation("SimulatedPlcClient disposed.");
            return ValueTask.CompletedTask;
        }

        public void Dispose()
        {
            // Left empty as DisposeAsync is the primary disposal method
        }

        public async Task HandshakeAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Simulating PLC handshake...");
            await Task.Delay(150, ct);
            _logger.LogInformation("Simulated PLC handshake successful.");
        }

        public async Task<string> GetBootloaderVersionAsync(CancellationToken ct = default)
        {
            _logger.LogInformation("Simulating getting bootloader version...");
            await Task.Delay(50, ct);
            var version = "SIM-1.2.3";
            _logger.LogInformation("Simulated bootloader version is {Version}.", version);
            return version;
        }

        public async Task InstallStagerAsync(byte[] stager, CancellationToken ct = default)
        {
            if (stager == null || stager.Length == 0)
            {
                throw new ArgumentNullException(nameof(stager));
            }
            _logger.LogInformation("Simulating stager installation with {StagerLength} bytes.", stager.Length);
            await Task.Delay(300, ct);
            _logger.LogInformation("Simulated stager installation successful.");
        }

        public async Task<byte[]> DumpMemoryAsync(uint startAddress, uint length, byte[] dumperPayload, IProgress<long> progress, CancellationToken ct = default)
        {
            _logger.LogInformation("Simulating memory dump from {Start} of length {Length}.", startAddress, length);
            if (dumperPayload != null && dumperPayload.Length > 0)
            {
                _logger.LogInformation("Simulating writing dumper payload of {DumperLength} bytes.", dumperPayload.Length);
                await Task.Delay(100, ct);
            }

            var buffer = new byte[length];
            var random = new Random();
            random.NextBytes(buffer);

            long bytesRead = 0;
            const int chunkSize = 2048;
            while (bytesRead < length)
            {
                ct.ThrowIfCancellationRequested();
                var chunk = Math.Min(chunkSize, length - bytesRead);
                await Task.Delay(25, ct);
                bytesRead += chunk;
                progress?.Report(bytesRead);
                _logger.LogDebug("Simulated memory dump progress: {BytesRead}/{Length}", bytesRead, length);
            }

            _logger.LogInformation("Simulated memory dump complete.");
            return buffer;
        }
    }
}
