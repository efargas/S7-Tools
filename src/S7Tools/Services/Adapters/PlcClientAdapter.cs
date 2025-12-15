using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Adapters.Plc;

namespace S7Tools.Services.Adapters
{
    /// <summary>
    /// Real implementation of PlcClientAdapter.
    /// Orchestrates the entire bootloader flow (Handshake -> Install Stager -> Dump Memory)
    /// utilizing the refactored, SOLID-compliant PLC components.
    /// </summary>
    public sealed class PlcClientAdapter : IPlcClient
    {
        private readonly ILogger<PlcClientAdapter> _logger;
        private readonly IPlcProtocol _protocol;

        // Components
        private readonly PlcProtocolHandler _protocolHandler;
        private readonly PlcMemoryManager _memoryManager;
        private readonly PlcStagerManager _stagerManager;

        public PlcClientAdapter(IPlcProtocol protocol, ILogger<PlcClientAdapter> logger)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize SOLID components
            _protocolHandler = new PlcProtocolHandler(protocol);
            _memoryManager = new PlcMemoryManager(_protocolHandler);
            _stagerManager = new PlcStagerManager(_protocolHandler, _memoryManager);
        }

        public void Configure(string host, int port)
        {
            _protocol.Configure(host, port);
        }

        public async ValueTask DisposeAsync()
        {
            if (_protocol is IAsyncDisposable d)
            {
                await d.DisposeAsync();
            }
        }

        #region Handshake

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Connecting to PLC via Protocol...");
            await _protocol.ConnectAsync(cancellationToken);
        }

        public async Task HandshakeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting handshake...");
            await _protocolHandler.PerformHandshakeAsync(cancellationToken);
            _logger.LogInformation("Handshake successful!");
        }

        public async Task<string> GetBootloaderVersionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting bootloader version...");
            return await _protocolHandler.GetVersionAsync(cancellationToken);
        }

        #endregion

        #region Stager Installation

        public async Task InstallStagerAsync(byte[] stager, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Installing Stager...");
            await _stagerManager.InstallStagerAsync(stager, cancellationToken);
            _logger.LogInformation("Stager installed at 0x{Addr:X}", PlcConstants.IRAM_STAGER_START);
        }

        #endregion

        #region Memory Dump

        public async Task<byte[]> DumpMemoryAsync(uint address, uint length, byte[] dumpPayload, IProgress<long> progress, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting Memory Dump via Stager...");
            return await _memoryManager.DumpMemoryAsync(address, length, dumpPayload, _stagerManager, progress, cancellationToken);
        }

        #endregion
    }
}
