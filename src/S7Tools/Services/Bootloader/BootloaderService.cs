using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// Orchestrates the complete bootloader memory dump workflow.
/// Coordinates socat bridge setup, power cycling, PLC communication, and memory dumping.
/// </summary>
public sealed class BootloaderService : IBootloaderService
{
    private readonly ILogger<BootloaderService> _logger;
    private readonly IPayloadProvider _payloads;
    private readonly ISocatService _socat;
    private readonly IPowerSupplyService _power;
    private readonly Func<JobProfileSet, IPlcClient> _clientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="payloads">Payload provider for stager and dumper files.</param>
    /// <param name="socat">Socat service for serial-to-TCP bridge management.</param>
    /// <param name="power">Power supply service for PLC power control.</param>
    /// <param name="clientFactory">Factory method for creating PLC client instances.</param>
    public BootloaderService(
        ILogger<BootloaderService> logger,
        IPayloadProvider payloads,
        ISocatService socat,
        IPowerSupplyService power,
        Func<JobProfileSet, IPlcClient> clientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
        _socat = socat ?? throw new ArgumentNullException(nameof(socat));
        _power = power ?? throw new ArgumentNullException(nameof(power));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    /// <inheritdoc />
    public async Task<byte[]> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent)> progress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);

        _logger.LogInformation("Starting bootloader dump operation");

        try
        {
            // Stage 1: Setup socat bridge (5% progress)
            progress.Report(("socat_setup", 0.05));
            _logger.LogDebug("Setting up socat bridge on port {Port}", profiles.Socat.Port);

            // Create socat configuration for the bridge
            var socatConfig = new SocatConfiguration
            {
                TcpPort = profiles.Socat.Port,
                Verbose = true,
                HexDump = false,
                BlockSize = 4,
                DebugLevel = 2,
                EnableFork = true,
                EnableReuseAddr = true,
                SerialRawMode = true,
                SerialDisableEcho = true
            };

            await _socat.StartSocatAsync(
                socatConfig,
                profiles.Serial.Device,
                cancellationToken).ConfigureAwait(false);

            // Stage 2: Power cycle PLC (10% progress)
            progress.Report(("power_cycle", 0.10));
            _logger.LogDebug("Power cycling PLC at {Host}:{Port} coil {Coil}",
                profiles.Power.Host, profiles.Power.Port, profiles.Power.Coil);

            await _power.PowerCycleAsync(
                profiles.Power.Host,
                profiles.Power.Port,
                profiles.Power.Coil,
                profiles.Power.DelaySeconds,
                cancellationToken).ConfigureAwait(false);

            // Stage 3: Create PLC client and perform handshake (20% progress)
            await using var client = _clientFactory(profiles);

            progress.Report(("handshake", 0.20));
            _logger.LogDebug("Performing bootloader handshake");

            await client.HandshakeAsync(cancellationToken).ConfigureAwait(false);

            var version = await client.GetBootloaderVersionAsync(cancellationToken)
                .ConfigureAwait(false);
            _logger.LogInformation("Connected to bootloader version: {Version}", version);

            // Stage 4: Install stager (30% progress)
            progress.Report(("stager_install", 0.30));
            _logger.LogDebug("Installing stager payload");

            var stagerPayload = await _payloads.GetStagerAsync(
                profiles.Payloads.BasePath,
                cancellationToken).ConfigureAwait(false);

            await client.InstallStagerAsync(stagerPayload, cancellationToken)
                .ConfigureAwait(false);

            // Stage 5: Dump memory (50% - 95% progress)
            progress.Report(("memory_dump", 0.50));
            _logger.LogDebug("Dumping memory region 0x{Address:X8} - 0x{EndAddress:X8} ({Length} bytes)",
                profiles.Memory.Start,
                profiles.Memory.Start + profiles.Memory.Length,
                profiles.Memory.Length);

            var dumperPayload = await _payloads.GetMemoryDumperAsync(
                profiles.Payloads.BasePath,
                cancellationToken).ConfigureAwait(false);

            var dumpProgress = new Progress<long>(bytesRead =>
            {
                var percent = 0.50 + (0.45 * bytesRead / profiles.Memory.Length);
                progress.Report(("memory_dump", percent));
            });

            var memoryData = await client.DumpMemoryAsync(
                profiles.Memory.Start,
                profiles.Memory.Length,
                dumperPayload,
                dumpProgress,
                cancellationToken).ConfigureAwait(false);

            // Stage 6: Teardown (95% progress)
            progress.Report(("teardown", 0.95));
            _logger.LogDebug("Cleaning up resources");

            // Client will be disposed automatically via 'await using'

            // Stage 7: Complete (100% progress)
            progress.Report(("complete", 1.0));
            _logger.LogInformation("Bootloader dump operation completed successfully. " +
                "Dumped {ByteCount} bytes", memoryData.Length);

            return memoryData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootloader dump operation failed: {ErrorMessage}", ex.Message);
            throw;
        }
    }
}
