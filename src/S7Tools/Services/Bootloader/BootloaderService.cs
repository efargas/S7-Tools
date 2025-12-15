using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Validation;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Resources;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// Orchestrates the complete bootloader memory dump workflow.
/// Coordinates serial configuration, socat bridge setup, power sequencing, PLC communication, and memory dumping.
/// </summary>
public sealed class BootloaderService : IBootloaderService
{
    private readonly ILogger<BootloaderService> _logger;
    private readonly IPayloadProvider _payloads;
    private readonly ISocatService _socat;
    private readonly IPowerSupplyService _power;
    private readonly ISerialPortService _serialPort;
    private readonly Func<JobProfileSet, IPlcClient> _clientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="payloads">Payload provider for stager and dumper files.</param>
    /// <param name="socat">Socat service for serial-to-TCP bridge management.</param>
    /// <param name="power">Power supply service for PLC power control.</param>
    /// <param name="serialPort">Serial port service for device configuration.</param>
    /// <param name="clientFactory">Factory method for creating PLC client instances.</param>
    public BootloaderService(
        ILogger<BootloaderService> logger,
        IPayloadProvider payloads,
        ISocatService socat,
        IPowerSupplyService power,
        ISerialPortService serialPort,
        Func<JobProfileSet, IPlcClient> clientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
        _socat = socat ?? throw new ArgumentNullException(nameof(socat));
        _power = power ?? throw new ArgumentNullException(nameof(power));
        _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    /// <inheritdoc />
    public async Task<byte[]> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent)> progress,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        Microsoft.Extensions.Logging.ILogger? processLogger = null,
        Microsoft.Extensions.Logging.ILogger? protocolLogger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);

        // Use taskLogger for task-level operations, fallback to main logger
        var effectiveTaskLogger = taskLogger ?? _logger;
        
        _logger.LogInformation("Starting bootloader dump operation");
        effectiveTaskLogger.LogInformation("=== BOOTLOADER DUMP OPERATION STARTED ===");
        effectiveTaskLogger.LogInformation("Serial: {Device} @ {Baud} baud", profiles.Serial.Device, profiles.Serial.Baud);
        effectiveTaskLogger.LogInformation("Socat: TCP port {Port}", profiles.Socat.Port);
        effectiveTaskLogger.LogInformation("Power: {Host}:{Port}", profiles.Power.Host, profiles.Power.Port);
        ulong start = profiles.Memory.Start;
        ulong length = (ulong)profiles.Memory.Length;
        ulong endExclusive = start + length;

        effectiveTaskLogger.LogInformation("Memory: 0x{Start:X8} - 0x{End:X8} ({Length} bytes)",
            start, endExclusive, profiles.Memory.Length);

        SocatProcessInfo? socatProcess = null;

        try
        {
            // Stage 0: Configure serial port (2% progress)
            progress.Report(("serial_config", 0.02));
            effectiveTaskLogger.LogInformation("--- Stage 0: Serial Port Configuration ---");
            effectiveTaskLogger.LogDebug("Configuring serial port {Device} with profile configuration", profiles.Serial.Device);
            effectiveTaskLogger.LogDebug("Configuration: {Baud} baud, {Parity}, {DataBits}-{StopBits}",
                profiles.Serial.Configuration.BaudRate,
                profiles.Serial.Configuration.Parity,
                profiles.Serial.Configuration.CharacterSize,
                profiles.Serial.Configuration.StopBits);

            // Use serial configuration directly from profile
            bool serialConfigured = await _serialPort.ApplyConfigurationAsync(
                profiles.Serial.Device,
                profiles.Serial.Configuration,
                effectiveTaskLogger,
                cancellationToken).ConfigureAwait(false);

            if (!serialConfigured)
            {
                throw new InvalidOperationException($"Failed to configure serial port {profiles.Serial.Device}");
            }

            effectiveTaskLogger.LogInformation("✓ Serial port {Device} configured successfully", profiles.Serial.Device);

            // Stage 1: Setup socat bridge (5% progress)
            progress.Report(("socat_setup", 0.05));
            effectiveTaskLogger.LogInformation("--- Stage 1: Socat Bridge Setup ---");
            effectiveTaskLogger.LogDebug("Setting up socat bridge on port {Port}", profiles.Socat.Port);

            // Use socat configuration directly from profile (must be non-null after Phase 2 changes)
            if (profiles.Socat.Configuration == null)
            {
                throw new InvalidOperationException("Socat configuration is required but was null. Ensure job profile includes full socat configuration.");
            }

            // Sync baud rate from serial profile to socat configuration to ensure correct speed
            profiles.Socat.Configuration.BaudRate = profiles.Serial.Baud;

            effectiveTaskLogger.LogDebug("Socat configuration: TCP:{Port} ⟷ {SerialDevice} @ {Baud} baud",
                profiles.Socat.Port, profiles.Serial.Device, profiles.Socat.Configuration.BaudRate);
            effectiveTaskLogger.LogDebug("Socat options: Verbose={Verbose}, HexDump={HexDump}, BlockSize={BlockSize}",
                profiles.Socat.Configuration.Verbose, profiles.Socat.Configuration.HexDump, profiles.Socat.Configuration.BlockSize);

            socatProcess = await _socat.StartSocatAsync(
                profiles.Socat.Configuration,
                profiles.Serial.Device,
                processLogger,
                protocolLogger,
                cancellationToken).ConfigureAwait(false);

            effectiveTaskLogger.LogInformation("✓ Socat bridge started on TCP port {Port} (PID: {ProcessId})",
                profiles.Socat.Port, socatProcess.ProcessId);

            // Stage 2: Connect to power supply (8% progress)
            progress.Report(("power_connect", 0.08));
            effectiveTaskLogger.LogInformation("--- Stage 2: Power Supply Connection ---");
            effectiveTaskLogger.LogDebug("Connecting to power supply at {Host}:{Port}",
                profiles.Power.Host, profiles.Power.Port);

            // Use power configuration directly from profile (must be non-null after Phase 2 changes)
            if (profiles.Power.Configuration == null)
            {
                throw new InvalidOperationException("Power supply configuration is required but was null. Ensure job profile includes full power supply configuration.");
            }

            bool connected = await _power.ConnectAsync(profiles.Power.Configuration, effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
            if (!connected)
            {
                throw new InvalidOperationException(UIStrings.Exception_FailedToConnectToPowerSupply);
            }

            effectiveTaskLogger.LogInformation("✓ Connected to power supply at {Host}:{Port}",
                profiles.Power.Host, profiles.Power.Port);

            try
            {
                // Stage 3: Power ON PLC (10% progress)
                progress.Report(("power_on", 0.10));
                effectiveTaskLogger.LogInformation("--- Stage 3: Power ON PLC ---");
                effectiveTaskLogger.LogDebug("Turning PLC power ON");

                bool powerOn = await _power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
                if (!powerOn)
                {
                    throw new InvalidOperationException("Failed to turn PLC power ON");
                }

                effectiveTaskLogger.LogInformation("✓ PLC powered ON");

                // Wait for initial power-on stabilization using PowerOnTimeMs from job profile
                effectiveTaskLogger.LogDebug("Waiting {DelayMs}ms for PLC power stabilization", profiles.PowerOnTimeMs);
                await Task.Delay(profiles.PowerOnTimeMs, cancellationToken).ConfigureAwait(false);
                effectiveTaskLogger.LogDebug("Power stabilization complete");

                // Stage 4: Create PLC client and CONNECT to socat (12% progress)
                // We connect BEFORE power cycling to ensure the serial port is open and ready.
                // This eliminates the ~1-2s latency of socat/forking that causes us to miss the 500ms handshake window.
                progress.Report(("plc_connect", 0.12));
                effectiveTaskLogger.LogInformation("--- Stage 4: PLC Client Connection ---");
                await using IPlcClient client = _clientFactory(profiles);

                // Set protocol logger for detailed communication logging
                client.SetProtocolLogger(protocolLogger);

                effectiveTaskLogger.LogDebug("PLC client created. Establishing connection to socat TCP server...");
                effectiveTaskLogger.LogDebug("Target: localhost:{Port}", profiles.Socat.Port);

                await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                effectiveTaskLogger.LogInformation("✓ PLC client connected to socat (Ready for Handshake)");

                // Stage 5: Power cycle PLC (15% progress)
                progress.Report(("power_cycle", 0.15));
                effectiveTaskLogger.LogInformation("--- Stage 5: Power Cycle PLC ---");
                effectiveTaskLogger.LogDebug("Power cycling PLC: OFF → wait {PowerOffDelayMs}ms → ON", profiles.PowerOffDelayMs);

                // Power cycle: OFF → delay → ON (using PowerOffDelayMs from job profile)
                await _power.PowerCycleAsync(profiles.PowerOffDelayMs, effectiveTaskLogger, cancellationToken)
                    .ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ PLC power cycled successfully (Client already connected)");


                // Stage 6: Perform handshake (20% progress)
                progress.Report(("handshake", 0.20));
                effectiveTaskLogger.LogInformation("--- Stage 6: Bootloader Handshake ---");
                effectiveTaskLogger.LogDebug("Performing bootloader handshake");

                // client is already connected; HandshakeAsync will just perform the protocol handshake immediately.
                await client.HandshakeAsync(cancellationToken).ConfigureAwait(false);

                string version = await client.GetBootloaderVersionAsync(cancellationToken)
                    .ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ Connected to bootloader version: {Version}", version);

                // Stage 7: Install stager (30% progress)
                progress.Report(("stager_install", 0.30));
                effectiveTaskLogger.LogInformation("--- Stage 7: Install Stager Payload ---");
                effectiveTaskLogger.LogDebug("Loading stager payload from {BasePath}", profiles.Payloads.BasePath);

                byte[] stagerPayload = await _payloads.GetStagerAsync(
                    profiles.Payloads.BasePath,
                    cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogDebug("Stager payload loaded: {Size} bytes", stagerPayload.Length);
                effectiveTaskLogger.LogDebug("Installing stager to PLC...");

                await client.InstallStagerAsync(stagerPayload, cancellationToken)
                    .ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ Stager payload installed successfully ({Size} bytes)", stagerPayload.Length);

                // Stage 8: Dump memory (50% - 95% progress)
                effectiveTaskLogger.LogInformation("--- Stage 8: Memory Dump ---");
                byte[] memoryData;

                if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
                {
                    // Multi-segment dump using MemoryMappingProfile
                    var selectedSegments = profiles.MemoryMapping.SelectedSegments.ToList();
                    effectiveTaskLogger.LogInformation("Multi-segment dump: {SegmentCount} segments from profile '{ProfileName}'",
                        selectedSegments.Count, profiles.MemoryMapping.Name);
                    effectiveTaskLogger.LogInformation("Total size: {TotalSize:N0} bytes ({TotalSizeKB:F2} KB)",
                        profiles.MemoryMapping.TotalSelectedSize, profiles.MemoryMapping.TotalSelectedSize / 1024.0);

                    var segmentDataList = new List<byte[]>();
                    long totalBytesRead = 0;
                    long totalSize = profiles.MemoryMapping.TotalSelectedSize;

                    byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                        profiles.Payloads.BasePath,
                        cancellationToken).ConfigureAwait(false);

                    effectiveTaskLogger.LogDebug("Memory dumper payload loaded: {Size} bytes", dumperPayload.Length);

                    var dumpStartTime = DateTime.UtcNow;

                    for (int i = 0; i < selectedSegments.Count; i++)
                    {
                        MemorySegment segment = selectedSegments[i];
                        uint segmentStart = uint.Parse(segment.StartAddress.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                        uint segmentSize = (uint)segment.Size;

                        progress.Report(("memory_dump", 0.50 + (0.45 * totalBytesRead / totalSize)));
                        effectiveTaskLogger.LogInformation("Dumping segment {Index}/{Total}: '{Name}'", i + 1, selectedSegments.Count, segment.Name);
                        effectiveTaskLogger.LogDebug("  Address: 0x{Address:X8}, Size: {Size:N0} bytes ({SizeKB:F2} KB)",
                            segmentStart, segmentSize, segmentSize / 1024.0);

                        bool logged25 = false, logged50 = false, logged75 = false;

                        var segmentProgress = new Progress<long>(bytesRead =>
                        {
                            double percent = 0.50 + (0.45 * (totalBytesRead + bytesRead) / totalSize);
                            progress.Report(("memory_dump", percent));

                            if (segmentSize == 0)
                            {
                                return;
                            }

                            // Log progress at 25%, 50%, 75% milestones (once each)
                            double segmentPercent = (double)bytesRead / segmentSize * 100.0;

                            if (!logged25 && segmentPercent >= 25.0)
                            {
                                logged25 = true;
                                effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                    bytesRead, segmentSize, segmentPercent);
                            }
                            else if (!logged50 && segmentPercent >= 50.0)
                            {
                                logged50 = true;
                                effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                    bytesRead, segmentSize, segmentPercent);
                            }
                            else if (!logged75 && segmentPercent >= 75.0)
                            {
                                logged75 = true;
                                effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                    bytesRead, segmentSize, segmentPercent);
                            }
                        });

                        var segmentStartTime = DateTime.UtcNow;
                        byte[] segmentData = await client.DumpMemoryAsync(
                            segmentStart,
                            segmentSize,
                            dumperPayload,
                            segmentProgress,
                            cancellationToken).ConfigureAwait(false);

                        var segmentDuration = DateTime.UtcNow - segmentStartTime;
                        var transferRate = segmentData.Length / segmentDuration.TotalSeconds;

                        segmentDataList.Add(segmentData);
                        totalBytesRead += segmentData.Length;

                        effectiveTaskLogger.LogInformation("  ✓ Segment '{Name}' dumped: {Size:N0} bytes in {Duration:F1}s ({Rate:F1} bytes/s)",
                            segment.Name, segmentData.Length, segmentDuration.TotalSeconds, transferRate);
                    }

                    // Concatenate all segment data
                    memoryData = segmentDataList.SelectMany(arr => arr).ToArray();
                    var totalDuration = DateTime.UtcNow - dumpStartTime;
                    var overallRate = memoryData.Length / totalDuration.TotalSeconds;
                    
                    effectiveTaskLogger.LogInformation("✓ Multi-segment dump completed: {TotalSegments} segments, {TotalSize:N0} bytes",
                        selectedSegments.Count, memoryData.Length);
                    effectiveTaskLogger.LogInformation("  Duration: {Duration:F1}s, Average rate: {Rate:F1} bytes/s ({RateKB:F1} KB/s)",
                        totalDuration.TotalSeconds, overallRate, overallRate / 1024.0);
                }
                else
                {
                    // Single-region dump using legacy MemoryRegionProfile
                    progress.Report(("memory_dump", 0.50));
                    effectiveTaskLogger.LogInformation("Single-region dump");
                    effectiveTaskLogger.LogDebug("Memory region: 0x{Address:X8} - 0x{EndAddress:X8}",
                        profiles.Memory.Start, profiles.Memory.Start + profiles.Memory.Length);
                    effectiveTaskLogger.LogDebug("Size: {Length:N0} bytes ({LengthKB:F2} KB)",
                        profiles.Memory.Length, profiles.Memory.Length / 1024.0);

                    byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                        profiles.Payloads.BasePath,
                        cancellationToken).ConfigureAwait(false);

                    effectiveTaskLogger.LogDebug("Memory dumper payload loaded: {Size} bytes", dumperPayload.Length);

                    bool logged25 = false, logged50 = false, logged75 = false;

                    var dumpProgress = new Progress<long>(bytesRead =>
                    {
                        double percent = 0.50 + (0.45 * bytesRead / profiles.Memory.Length);
                        progress.Report(("memory_dump", percent));
    
                        // Log progress at 25%, 50%, 75% milestones (once each)
                        double dumpPercent = (double)bytesRead / profiles.Memory.Length * 100;
    
                        if (!logged25 && dumpPercent >= 25.0)
                        {
                            logged25 = true;
                            effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                bytesRead, profiles.Memory.Length, dumpPercent);
                        }
                        else if (!logged50 && dumpPercent >= 50.0)
                        {
                            logged50 = true;
                            effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                bytesRead, profiles.Memory.Length, dumpPercent);
                        }
                        else if (!logged75 && dumpPercent >= 75.0)
                        {
                            logged75 = true;
                            effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                bytesRead, profiles.Memory.Length, dumpPercent);
                        }
                    });

                    var dumpStartTime = DateTime.UtcNow;
                    memoryData = await client.DumpMemoryAsync(
                        profiles.Memory.Start,
                        profiles.Memory.Length,
                        dumperPayload,
                        dumpProgress,
                        cancellationToken).ConfigureAwait(false);

                    var dumpDuration = DateTime.UtcNow - dumpStartTime;
                    var transferRate = memoryData.Length / dumpDuration.TotalSeconds;

                    effectiveTaskLogger.LogInformation("✓ Memory dump completed: {Size:N0} bytes from 0x{Start:X8}",
                        memoryData.Length, profiles.Memory.Start);
                    effectiveTaskLogger.LogInformation("  Duration: {Duration:F1}s, Transfer rate: {Rate:F1} bytes/s ({RateKB:F1} KB/s)",
                        dumpDuration.TotalSeconds, transferRate, transferRate / 1024.0);
                }

                // Stage 9: Teardown (95% progress)
                progress.Report(("teardown", 0.95));
                effectiveTaskLogger.LogInformation("--- Stage 9: Teardown ---");
                effectiveTaskLogger.LogDebug("Cleaning up PLC client resources...");

                // Client will be disposed automatically via 'await using'

                // Stage 10: Complete (100% progress)
                progress.Report(("complete", 1.0));
                effectiveTaskLogger.LogInformation("=== BOOTLOADER DUMP OPERATION COMPLETED ===");
                effectiveTaskLogger.LogInformation("✓ Successfully dumped {ByteCount:N0} bytes ({ByteCountKB:F2} KB)",
                    memoryData.Length, memoryData.Length / 1024.0);

                return memoryData;
            }
            finally
            {
                // Always disconnect from power supply
                effectiveTaskLogger?.LogDebug("Disconnecting from power supply...");
                await _power.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                effectiveTaskLogger?.LogDebug("✓ Disconnected from power supply");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootloader dump operation failed: {ErrorMessage}", ex.Message);
            effectiveTaskLogger?.LogError("❌ DUMP OPERATION FAILED: {ErrorMessage}", ex.Message);
            effectiveTaskLogger?.LogError("Exception type: {ExceptionType}", ex.GetType().Name);
            throw;
        }
        finally
        {
            if (socatProcess != null)
            {
                effectiveTaskLogger?.LogDebug("Stopping socat process (PID: {ProcessId})...", socatProcess.ProcessId);
                await _socat.StopSocatAsync(socatProcess, cancellationToken).ConfigureAwait(false);
                effectiveTaskLogger?.LogDebug("✓ Stopped socat process");
            }
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateProfileSetAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var errors = new List<string>();

        // Validate serial port accessibility
        if (!string.IsNullOrWhiteSpace(profiles.Serial.Device))
        {
            bool isAccessible = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
                ? File.Exists(profiles.Serial.Device)
                : System.IO.Ports.SerialPort.GetPortNames().Contains(profiles.Serial.Device);

            if (!isAccessible)
            {
                errors.Add($"Serial port '{profiles.Serial.Device}' is not accessible");
            }
        }
        else
        {
            errors.Add("Serial port device is not specified");
        }

        // Validate TCP port availability
        try
        {
            using var listener = new System.Net.Sockets.TcpListener(
                System.Net.IPAddress.Loopback,
                profiles.Socat.Port);
            listener.Start();
            listener.Stop();
        }
        catch (System.Net.Sockets.SocketException)
        {
            errors.Add($"TCP port {profiles.Socat.Port} is already in use");
        }

        // Validate modbus host reachability (with 5s timeout)
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var tcpClient = new System.Net.Sockets.TcpClient();
            await tcpClient.ConnectAsync(
                profiles.Power.Host,
                profiles.Power.Port,
                cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            errors.Add($"Modbus host '{profiles.Power.Host}:{profiles.Power.Port}' is not reachable: {ex.Message}");
        }

        // Validate payloads exist
        try
        {
            await _payloads.GetStagerAsync(profiles.Payloads.BasePath, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            errors.Add($"Stager payload not found at '{profiles.Payloads.BasePath}'");
        }

        try
        {
            await _payloads.GetMemoryDumperAsync(profiles.Payloads.BasePath, cancellationToken).ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            errors.Add($"Memory dumper payload not found at '{profiles.Payloads.BasePath}'");
        }

        // Validate memory region (check for overflow)
        try
        {
            checked
            {
                uint endAddress = profiles.Memory.Start + profiles.Memory.Length;
                if (endAddress < profiles.Memory.Start)
                {
                    errors.Add($"Memory region overflow: start=0x{profiles.Memory.Start:X8}, length=0x{profiles.Memory.Length:X8}");
                }
            }
        }
        catch (OverflowException)
        {
            errors.Add($"Memory region overflow: start=0x{profiles.Memory.Start:X8}, length=0x{profiles.Memory.Length:X8}");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.Select(e => new ValidationError("ProfileSet", e)).ToArray());
    }

    /// <inheritdoc />
    public TimeSpan EstimateDuration(MemoryRegionProfile memoryRegion)
    {
        ArgumentNullException.ThrowIfNull(memoryRegion);

        // Base overhead: 15 seconds
        // Transfer rate: 256 bytes/sec (conservative estimate)
        const double baseOverheadSeconds = 15.0;
        const double bytesPerSecond = 256.0;

        double transferTime = memoryRegion.Length / bytesPerSecond;
        double totalSeconds = baseOverheadSeconds + transferTime;

        // Clamp to 5-300s range per SC-001
        totalSeconds = Math.Clamp(totalSeconds, 5.0, 300.0);

        return TimeSpan.FromSeconds(totalSeconds);
    }

    #region Helper Methods


    #endregion
}
