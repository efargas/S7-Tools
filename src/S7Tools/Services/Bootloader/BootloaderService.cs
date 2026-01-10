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
public sealed class BootloaderService(
    ILogger<BootloaderService> logger,
    IPayloadProvider payloads,
    ISocatService socat,
    IPowerSupplyService power,
    ISerialPortService serialPort,
    ITimeProvider timeProvider,
    Func<JobProfileSet, IPlcClient> clientFactory) : IBootloaderService
{
    private readonly ILogger<BootloaderService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPayloadProvider _payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
    private readonly ISocatService _socat = socat ?? throw new ArgumentNullException(nameof(socat));
    private readonly IPowerSupplyService _power = power ?? throw new ArgumentNullException(nameof(power));
    private readonly ISerialPortService _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
    private readonly Func<JobProfileSet, IPlcClient> _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    private const int InitialPowerOffWaitMs = 10000;

    /// <inheritdoc />
    public async Task<byte[]> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        Microsoft.Extensions.Logging.ILogger? processLogger = null,
        Microsoft.Extensions.Logging.ILogger? protocolLogger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);

        // Use taskLogger for task-level operations, fallback to main logger
        Microsoft.Extensions.Logging.ILogger effectiveTaskLogger = taskLogger ?? _logger;

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
            progress.Report(("serial_config", 2.0, null, null));
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
            progress.Report(("socat_setup", 5.0, null, null));
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
            progress.Report(("power_connect", 8.0, null, null));
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
                // Stage 3: Power OFF PLC and wait (8% -> 9% progress)
                progress.Report(("power_off_initial", 9.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 3: Initial Power OFF ---");
                effectiveTaskLogger.LogDebug("Turning PLC power OFF and waiting {WaitMs}ms", InitialPowerOffWaitMs);

                bool powerOff = await _power.TurnOffAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
                if (!powerOff)
                {
                    // Log warning but continue? Or throw? Assuming safe to throw if we can't ensure off state.
                    // For robustness, let's treat failure to turn off as critical if we expect a clean slate.
                    // However, if it's already off, TurnOffAsync might return true or false depending on implementation.
                    // Assuming TurnOffAsync returns success of the command.
                    throw new InvalidOperationException("Failed to turn PLC power OFF");
                }

                await WaitWithProgressAsync(
                    InitialPowerOffWaitMs,
                    progress,
                    9.0, 15.0, // 9% to 15% during wait
                    "power_off_initial",
                    cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ PLC powered OFF and wait time completed");
                // Stage 4: Power ON PLC (15% progress)
                progress.Report(("power_on", 15.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 4: Power ON PLC ---");
                effectiveTaskLogger.LogDebug("Turning PLC power ON");

                bool powerOn = await _power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
                if (!powerOn)
                {
                    throw new InvalidOperationException("Failed to turn PLC power ON");
                }

                effectiveTaskLogger.LogInformation("✓ PLC powered ON");

                // Wait for initial power-on stabilization (15% -> 17%)
                effectiveTaskLogger.LogDebug("Waiting {DelayMs}ms for PLC power stabilization", profiles.PowerOnTimeMs);

                await WaitWithProgressAsync(
                    profiles.PowerOnTimeMs,
                    progress,
                    15.0, 17.0,
                    "power_on",
                    cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogDebug("Power stabilization complete");

                // Stage 5: Create PLC client and CONNECT to socat (17% progress)
                // We connect BEFORE power cycling to ensure the serial port is open and ready.
                // This eliminates the ~1-2s latency of socat/forking that causes us to miss the 500ms handshake window.
                progress.Report(("plc_connect", 17.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 5: PLC Client Connection ---");
                await using IPlcClient client = _clientFactory(profiles);

                // Set protocol logger for detailed communication logging
                client.SetProtocolLogger(protocolLogger);

                effectiveTaskLogger.LogDebug("PLC client created. Establishing connection to socat TCP server...");
                effectiveTaskLogger.LogDebug("Target: localhost:{Port}", profiles.Socat.Port);

                await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                effectiveTaskLogger.LogInformation("✓ PLC client connected to socat (Ready for Handshake)");

                // Stage 6: Power cycle PLC (20% progress)
                progress.Report(("power_cycle", 20.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 6: Power Cycle PLC ---");
                effectiveTaskLogger.LogDebug("Power cycling PLC: OFF → wait {PowerOffDelayMs}ms → ON", profiles.PowerOffDelayMs);

                // Decomposed Power Cycle for progress reporting
                await _power.TurnOffAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);

                //Keep this commented
                /*await WaitWithProgressAsync(
                    profiles.PowerOffDelayMs,
                    progress,
                    20.0, 25.0,
                    "power_cycle",
                    cancellationToken).ConfigureAwait(false);
                */
                await _power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ PLC power cycled successfully (Client already connected)");


                // Stage 7: Perform handshake (25% progress)
                progress.Report(("handshake", 25.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 7: Bootloader Handshake ---");
                effectiveTaskLogger.LogDebug("Performing bootloader handshake");

                // client is already connected; HandshakeAsync will just perform the protocol handshake immediately.
                await client.HandshakeAsync(cancellationToken).ConfigureAwait(false);

                string version = await client.GetBootloaderVersionAsync(cancellationToken)
                    .ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ Connected to bootloader version: {Version}", version);

                // Stage 8: Install stager (30% progress)
                progress.Report(("stager_install", 30.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 8: Install Stager Payload ---");
                effectiveTaskLogger.LogDebug("Loading stager payload from {BasePath}", profiles.Payloads.BasePath);

                byte[] stagerPayload = await _payloads.GetStagerAsync(
                    profiles.Payloads.BasePath,
                    cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogDebug("Stager payload loaded: {Size} bytes", stagerPayload.Length);
                effectiveTaskLogger.LogDebug("Installing stager to PLC...");

                await client.InstallStagerAsync(stagerPayload, cancellationToken)
                    .ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ Stager payload installed successfully ({Size} bytes)", stagerPayload.Length);

                // Stage 9: Dump memory (50% - 95% progress)
                effectiveTaskLogger.LogInformation("--- Stage 9: Memory Dump ---");
                byte[] memoryData;

                if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
                {
                    // Multi-segment dump using MemoryMappingProfile
                    var selectedSegments = profiles.MemoryMapping.SelectedSegments.ToList();
                    effectiveTaskLogger.LogInformation("Multi-segment dump: {SegmentCount} segments from profile '{ProfileName}'",
                        selectedSegments.Count, profiles.MemoryMapping.Name);
                    effectiveTaskLogger.LogInformation("Total size: {TotalSize:N0} bytes ({TotalSizeKB:F2} KB)",
                        profiles.MemoryMapping.TotalSelectedSize, profiles.MemoryMapping.TotalSelectedSize / 1024.0);

                    List<byte[]> segmentDataList = [];
                    long totalBytesRead = 0;
                    long totalSize = profiles.MemoryMapping.TotalSelectedSize;

                    byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                        profiles.Payloads.BasePath,
                        cancellationToken).ConfigureAwait(false);

                    effectiveTaskLogger.LogDebug("Memory dumper payload loaded: {Size} bytes", dumperPayload.Length);
                    DateTime dumpStartTime = _timeProvider.GetUtcNow();

                    for (int i = 0; i < selectedSegments.Count; i++)
                    {
                        MemorySegment segment = selectedSegments[i];
                        uint segmentStart = uint.Parse(segment.StartAddress.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                        uint segmentSize = (uint)segment.Size;
                        progress.Report(("memory_dump", 50.0 + (45.0 * totalBytesRead / totalSize), totalBytesRead, totalSize));
                        effectiveTaskLogger.LogInformation("Dumping segment {Index}/{Total}: '{Name}'", i + 1, selectedSegments.Count, segment.Name);
                        effectiveTaskLogger.LogDebug("  Address: 0x{Address:X8}, Size: {Size:N0} bytes ({SizeKB:F2} KB)",
                            segmentStart, segmentSize, segmentSize / 1024.0);

                        int lastLoggedPercent = -1;

                        double lastReportedPercent = 50.0 + (45.0 * totalBytesRead / totalSize);

                        var segmentProgress = new Progress<long>(bytesRead =>
                        {
                            double percent = 50.0 + (45.0 * (totalBytesRead + bytesRead) / totalSize);

                            // Only report progress if it has changed by at least 0.1% or if complete
                            if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesRead == segmentSize)
                            {
                                progress.Report(("memory_dump", percent, totalBytesRead + bytesRead, totalSize));
                                lastReportedPercent = percent;
                            }

                            if (segmentSize == 0)
                            {
                                return;
                            }

                            // Log progress at every 1% interval
                            double segmentPercent = (double)bytesRead / segmentSize * 100.0;
                            int currentPercent = (int)segmentPercent;

                            if (currentPercent > lastLoggedPercent && currentPercent % 1 == 0)
                            {
                                lastLoggedPercent = currentPercent;
                                effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                    bytesRead, segmentSize, segmentPercent);
                            }
                        });

                        DateTime segmentStartTime = _timeProvider.GetUtcNow();
                        byte[] segmentData = await client.DumpMemoryAsync(
                            segmentStart,
                            segmentSize,
                            dumperPayload,
                            segmentProgress,
                            cancellationToken).ConfigureAwait(false);

                        TimeSpan segmentDuration = _timeProvider.GetUtcNow() - segmentStartTime;
                        double transferRate = segmentData.Length / segmentDuration.TotalSeconds;

                        segmentDataList.Add(segmentData);
                        totalBytesRead += segmentData.Length;

                        effectiveTaskLogger.LogInformation("  ✓ Segment '{Name}' dumped: {Size:N0} bytes in {Duration:F1}s ({Rate:F1} bytes/s)",
                            segment.Name, segmentData.Length, segmentDuration.TotalSeconds, transferRate);
                    }

                    // Concatenate all segment data
                    memoryData = [.. segmentDataList.SelectMany(arr => arr)];
                    TimeSpan totalDuration = _timeProvider.GetUtcNow() - dumpStartTime;
                    double overallRate = memoryData.Length / totalDuration.TotalSeconds;

                    effectiveTaskLogger.LogInformation("✓ Multi-segment dump completed: {TotalSegments} segments, {TotalSize:N0} bytes",
                        selectedSegments.Count, memoryData.Length);
                    effectiveTaskLogger.LogInformation("  Duration: {Duration:F1}s, Average rate: {Rate:F1} bytes/s ({RateKB:F1} KB/s)",
                        totalDuration.TotalSeconds, overallRate, overallRate / 1024.0);
                }
                else
                {
                    // Single-region dump using legacy MemoryRegionProfile
                    progress.Report(("memory_dump", 50.0, 0, (long)profiles.Memory.Length));
                    effectiveTaskLogger.LogInformation("Single-region dump");
                    effectiveTaskLogger.LogDebug("Memory region: 0x{Address:X8} - 0x{EndAddress:X8}",
                        profiles.Memory.Start, profiles.Memory.Start + profiles.Memory.Length);
                    effectiveTaskLogger.LogDebug("Size: {Length:N0} bytes ({LengthKB:F2} KB)",
                        profiles.Memory.Length, profiles.Memory.Length / 1024.0);

                    byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                        profiles.Payloads.BasePath,
                        cancellationToken).ConfigureAwait(false);

                    effectiveTaskLogger.LogDebug("Memory dumper payload loaded: {Size} bytes", dumperPayload.Length);

                    int lastLoggedPercent = -1;

                    double lastReportedDumpPercent = 50.0;

                    var dumpProgress = new Progress<long>(bytesRead =>
                    {
                        if (profiles.Memory.Length == 0)
                        {
                            progress.Report(("memory_dump", 95.0, 0, 0)); // Report near-completion for zero-length dump
                            return;
                        }

                        double percent = 50.0 + (45.0 * bytesRead / profiles.Memory.Length);

                        // Only report progress if it has changed by at least 0.1%
                        if (Math.Abs(percent - lastReportedDumpPercent) >= 0.1 || bytesRead == profiles.Memory.Length)
                        {
                            progress.Report(("memory_dump", percent, bytesRead, (long)profiles.Memory.Length));
                            lastReportedDumpPercent = percent;
                        }

                        // Log progress at every 1% interval
                        double dumpPercent = (double)bytesRead / profiles.Memory.Length * 100.0;
                        int currentPercent = (int)dumpPercent;

                        if (currentPercent > lastLoggedPercent && currentPercent % 1 == 0)
                        {
                            lastLoggedPercent = currentPercent;
                            effectiveTaskLogger.LogDebug("  Progress: {BytesRead:N0}/{TotalSize:N0} bytes ({Percent:F1}%)",
                                bytesRead, profiles.Memory.Length, dumpPercent);
                        }
                    });

                    DateTime dumpStartTime = _timeProvider.GetUtcNow();
                    memoryData = await client.DumpMemoryAsync(
                        profiles.Memory.Start,
                        profiles.Memory.Length,
                        dumperPayload,
                        dumpProgress,
                        cancellationToken).ConfigureAwait(false);

                    TimeSpan dumpDuration = _timeProvider.GetUtcNow() - dumpStartTime;
                    double transferRate = memoryData.Length > 0 && dumpDuration.TotalSeconds > 0 ? memoryData.Length / dumpDuration.TotalSeconds : 0;

                    effectiveTaskLogger.LogInformation("✓ Memory dump completed: {Size:N0} bytes from 0x{Start:X8}",
                        memoryData.Length, profiles.Memory.Start);
                    effectiveTaskLogger.LogInformation("  Duration: {Duration:F1}s, Transfer rate: {Rate:F1} bytes/s ({RateKB:F1} KB/s)",
                        dumpDuration.TotalSeconds, transferRate, transferRate / 1024.0);
                }

                // Stage 10: Teardown (95% progress)
                progress.Report(("teardown", 95.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 10: Teardown ---");
                effectiveTaskLogger.LogDebug("Cleaning up PLC client resources...");

                // Client will be disposed automatically via 'await using'

                // Stage 11: Complete (100% progress)
                progress.Report(("complete", 100.0, null, null));
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
            effectiveTaskLogger?.LogError("DUMP OPERATION FAILED: {ErrorMessage}", ex.Message);
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
    private async Task WaitWithProgressAsync(
        int delayMs,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        double startPercent,
        double targetPercent,
        string stage,
        CancellationToken cancellationToken)
    {
        if (delayMs <= 0)
        {
            return;
        }

        // Update every 100ms
        int steps = delayMs / 100;
        if (steps <= 0)
        {
            steps = 1;
        }

        double increment = (targetPercent - startPercent) / steps;

        for (int i = 0; i < steps; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            double currentPercent = startPercent + (increment * (i + 1));
            progress.Report((stage, currentPercent, null, null));
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
            : ValidationResult.Failure([.. errors.Select(e => new ValidationError("ProfileSet", e))]);
    }

    /// <inheritdoc />
    public TimeSpan EstimateDuration(MemoryRegionProfile memoryRegion)
    {
        ArgumentNullException.ThrowIfNull(memoryRegion);

        // Base overhead: 15 seconds
        // Transfer rate: 256 bytes/sec (conservative estimate)
        const double BaseOverheadSeconds = 15.0;
        const double BytesPerSecond = 256.0;

        double transferTime = memoryRegion.Length / BytesPerSecond;
        double totalSeconds = BaseOverheadSeconds + transferTime;

        // Clamp to 5-300s range per SC-001
        totalSeconds = Math.Clamp(totalSeconds, 5.0, 300.0);

        return TimeSpan.FromSeconds(totalSeconds);
    }
}
