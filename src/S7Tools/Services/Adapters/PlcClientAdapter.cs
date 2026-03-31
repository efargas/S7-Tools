using S7Tools.Core.Interfaces.Services;
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
        private readonly MemoryDumpOrchestrator _orchestrator;

        // Socat connection info (set via Configure)
        private string _socatHost = "127.0.0.1";
        private int _socatPort = 3333; // Default fallback

        /// <summary>
        /// Initializes a new instance of the <see cref="PlcClientAdapter"/> class.
        /// </summary>
        public PlcClientAdapter(
            IPlcProtocol protocol,
            ILogger<PlcClientAdapter> logger,
            ILoggerFactory loggerFactory,
            MemoryDumpOrchestrator orchestrator)
        {
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            ArgumentNullException.ThrowIfNull(loggerFactory);

            // Initialize SOLID components
            _protocolHandler = new PlcProtocolHandler(protocol);
            _memoryManager = new PlcMemoryManager(_protocolHandler, _orchestrator, loggerFactory.CreateLogger<PlcMemoryManager>());
            _stagerManager = new PlcStagerManager(_protocolHandler, _memoryManager);
        }



        /// <summary>
        /// Executes the Configure operation.
        /// </summary>
        public void Configure(string host, int port)
        {
            // Store socat connection info for streaming dumps
            _socatHost = host;
            _socatPort = port;

            _protocol.Configure(host, port);
        }

        /// <summary>
        /// Sets an optional session-specific logger (e.g., for task-specific protocol logging).
        /// </summary>
        public void SetLogger(ILogger? logger)
        {
            _protocol.SetLogger(logger);
        }

        /// <summary>
        /// Executes the DisposeAsync operation.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_protocol is IAsyncDisposable d)
            {
                await d.DisposeAsync().ConfigureAwait(false);
            }
        }

        #region Handshake

        /// <summary>
        /// Executes the ConnectAsync operation.
        /// </summary>
        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Connecting to PLC via Protocol...");
            await _protocol.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the HandshakeAsync operation.
        /// </summary>
        public async Task HandshakeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting handshake...");
            await _protocolHandler.PerformHandshakeAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Handshake successful!");
        }

        /// <summary>
        /// Executes the GetBootloaderVersionAsync operation.
        /// </summary>
        public async Task<string> GetBootloaderVersionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting bootloader version...");
            byte[] versionBytes = await _protocolHandler.GetVersionAsync(cancellationToken).ConfigureAwait(false);

            if (versionBytes.Length == 0)
            {
                return "Unknown";
            }

            // Log raw bytes for debugging
            _logger.LogDebug("Version Response Hex: {Hex}", BitConverter.ToString(versionBytes));

            // Specific parsing for binary version format:
            // Look for 0x56 ('V') followed by 3 bytes (Major, Minor, Patch)
            // Example: ... 56 04 02 01 ... -> V4.02.1
            int vIndex = Array.IndexOf(versionBytes, (byte)0x56);
            if (vIndex >= 0 && vIndex + 3 < versionBytes.Length)
            {
                byte major = versionBytes[vIndex + 1];
                byte minor = versionBytes[vIndex + 2];
                byte patch = versionBytes[vIndex + 3];

                // Format: V{Major}.{Minor:00}.{Patch}
                string formatted = $"V{major}.{minor:D2}.{patch}";
                _logger.LogDebug("Decoded binary version: {Version}", formatted);
                return formatted;
            }

            // Fallback: Try decoding as UTF8 string if binary pattern not found
            try
            {
                // Filter to printable ASCII/UTF8 chars
                string raw = System.Text.Encoding.UTF8.GetString(versionBytes);
                // Keep only valid version characters (alphanumeric, dot, space, hyphen)
                // This strips out any control characters or nulls that might be confusing the output
                char[] validChars = [.. raw.Where(c =>
                    char.IsLetterOrDigit(c) ||
                    c == '.' ||
                    c == '-' ||
                    c == '_' ||
                    c == ' ')];

                string cleaned = new string(validChars).Trim();
                return string.IsNullOrEmpty(cleaned) ? "Unknown" : cleaned;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decode version string");
                return "DecodeError";
            }
        }

        #endregion

        #region Stager Installation

        /// <summary>
        /// Executes the InstallStagerAsync operation.
        /// </summary>
        public async Task InstallStagerAsync(byte[] stager, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Installing Stager...");
            await _stagerManager.InstallStagerAsync(stager, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Stager installed at 0x{Addr:X}", PlcConstants.IRAM_STAGER_START);
        }

        #endregion

        #region Memory Dump

        /// <summary>
        /// Executes the InstallDumperAsync operation.
        /// </summary>
        public async Task InstallDumperAsync(byte[] dumperPayload, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Installing Dumper Payload via Stager...");
            // Install to DUMPER_PAYLOAD_LOCATION and Hook 2
            await _stagerManager.InstallAddHookViaStagerAsync(
                PlcConstants.DUMPER_PAYLOAD_LOCATION,
                dumperPayload,
                PlcConstants.DEFAULT_SECOND_ADD_HOOK_IND,
                cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Dumper payload installed at 0x{Addr:X} (Hook {Hook})",
                PlcConstants.DUMPER_PAYLOAD_LOCATION, PlcConstants.DEFAULT_SECOND_ADD_HOOK_IND);
        }

        /// <summary>
        /// Executes the InvokeDumperAsync operation.
        /// </summary>
        public async Task<byte[]> InvokeDumperAsync(uint address, uint length, IProgress<long> progress, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Invoking Dumper (0x{Addr:X}, {Len} bytes)...", address, length);
            return await _memoryManager.InvokeDumperAsync(address, length, progress, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the InvokeDumperStreamAsync operation.
        /// </summary>
        public async Task InvokeDumperStreamAsync(
            uint address,
            uint length,
            Func<ReadOnlyMemory<byte>, ValueTask> dataCallback,
            IProgress<long> progress,
            CancellationToken cancellationToken = default,
            bool keepSessionOpen = false,
            ILogger? logger = null)
        {
            _logger.LogInformation("Invoking Dumper (Streaming) (0x{Addr:X}, {Len} bytes)...", address, length);

            // Use socat connection info from Configure() call
            await _memoryManager.InvokeDumperStreamAsync(
                address,
                length,
                dataCallback,
                progress,
                _socatHost,
                _socatPort,
                cancellationToken,
                keepSessionOpen,
                logger).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the StartDumperSessionAsync operation.
        /// </summary>
        public async Task StartDumperSessionAsync(CancellationToken cancellationToken = default, ILogger? logger = null)
        {
            _logger.LogInformation("Starting persistent dumper session...");
            await _memoryManager.StartDumperSessionAsync(_socatHost, _socatPort, cancellationToken, logger);
        }

        /// <summary>
        /// Executes the StopDumperSessionAsync operation.
        /// </summary>
        public async Task StopDumperSessionAsync()
        {
            _logger.LogInformation("Stopping persistent dumper session...");
            await _orchestrator.StopAsync().ConfigureAwait(false);
        }

        #endregion
    }
}
