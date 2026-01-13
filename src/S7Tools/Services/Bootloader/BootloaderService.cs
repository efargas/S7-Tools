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
    /// <inheritdoc />
    public async Task<IList<byte[]>> DumpAsync(
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
                    throw new InvalidOperationException("Failed to turn PLC power OFF");
                }

                await WaitWithProgressAsync(
                    InitialPowerOffWaitMs,
                    progress,
                    9.0, 15.0, // 9% to 15% during wait
                    "power_off_wait",
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

                // FIX: Use granular wait for Power On stabilization
                await WaitWithProgressAsync(
                    profiles.PowerOnTimeMs,
                    progress,
                    15.0, 17.0,
                    "power_on_stabilize",
                    cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogDebug("Power stabilization complete");

                // Stage 6: Create PLC client and CONNECT to socat (17% progress)
                // We connect BEFORE power cycling to ensure the serial port is open and ready.
                progress.Report(("plc_connect", 17.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 6: PLC Client Connection ---");
                await using IPlcClient client = _clientFactory(profiles);

                // Set protocol logger for detailed communication logging
                client.SetProtocolLogger(protocolLogger);

                effectiveTaskLogger.LogDebug("PLC client created. Establishing connection to socat TCP server...");
                effectiveTaskLogger.LogDebug("Target: localhost:{Port}", profiles.Socat.Port);

                await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                effectiveTaskLogger.LogInformation("✓ PLC client connected to socat (Ready for Handshake)");

                // Stage 7: Power cycle PLC (20% progress)
                progress.Report(("power_cycle", 20.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 7: Power Cycle PLC ---");
                effectiveTaskLogger.LogDebug("Power cycling PLC: OFF → wait {PowerOffDelayMs}ms → ON", profiles.PowerOffDelayMs);

                // Decomposed Power Cycle for progress reporting
                await _power.TurnOffAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);

                // Short wait between power cycle
                await WaitWithProgressAsync(
                    profiles.PowerOffDelayMs,
                    progress,
                    20.0, 22.0,
                    "power_cycle_wait",
                    cancellationToken).ConfigureAwait(false);

                await _power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ PLC power cycled successfully (Client already connected)");


                // Stage 8: Perform handshake (25% progress)
                progress.Report(("handshake", 22.0, null, null)); // Adjusted to start from 22
                effectiveTaskLogger.LogInformation("--- Stage 8: Bootloader Handshake ---");
                effectiveTaskLogger.LogDebug("Performing bootloader handshake");

                // client is already connected; HandshakeAsync will just perform the protocol handshake immediately.
                await client.HandshakeAsync(cancellationToken).ConfigureAwait(false);

                string version = await client.GetBootloaderVersionAsync(cancellationToken)
                    .ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ Connected to bootloader version: {Version}", version);

                // Stage 9: Install stager (25% progress)
                progress.Report(("stager_install", 25.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 9: Install Stager Payload ---");
                effectiveTaskLogger.LogDebug("Loading stager payload from {BasePath}", profiles.Payloads.BasePath);

                byte[] stagerPayload = await _payloads.GetStagerAsync(
                    profiles.Payloads.BasePath,
                    cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogDebug("Stager payload loaded: {Size} bytes", stagerPayload.Length);
                effectiveTaskLogger.LogDebug("Installing stager to PLC...");

                await client.InstallStagerAsync(stagerPayload, cancellationToken)
                    .ConfigureAwait(false);

                effectiveTaskLogger.LogInformation("✓ Stager payload installed successfully ({Size} bytes)", stagerPayload.Length);

                // Stage 10: Install Dumper Payload (28% progress)
                progress.Report(("dumper_install", 28.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 10: Install Memory Dumper Payload ---");

                byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                    profiles.Payloads.BasePath,
                    cancellationToken).ConfigureAwait(false);

                effectiveTaskLogger.LogDebug("Memory dumper payload loaded: {Size} bytes", dumperPayload.Length);
                effectiveTaskLogger.LogDebug("Installing dumper payload to PLC...");

                await client.InstallDumperAsync(dumperPayload, cancellationToken).ConfigureAwait(false);
                effectiveTaskLogger.LogInformation("✓ Dumper payload installed successfully");

                // Stage 11: Memory Dump (30% - 95% progress) = 65% weight? 
                // User asked for "Memory dump step would be 75% of all the task"
                // So if we start dump at 20%, we end at 95%. 
                // Let's adjust slightly:
                // Setup: 0-20%
                // Dump: 20-95% (75%)
                // Teardown: 95-100% (5%)

                // My current previous steps ended at 28%. Let's squeeze earlier steps or just accept slight deviation.
                // Let's re-map:
                // Setup (Serial/Socat): 0-5%
                // Power Setup (Connect, Off, Wait, On): 5-15%
                // Connect/Shake/Install: 15-20% (Fast steps)
                // Dump: 20-95%

                effectiveTaskLogger.LogInformation("--- Stage 11: Memory Dump ---");

                List<byte[]> allDumps = new();

                // Calculate total bytes expected across ALL iterations and segments
                long totalDumpBytes = 0;
                var segments = profiles.MemoryMapping?.SelectedSegments?.ToList() ?? [];

                // If no complex mapping, create a default segment from the basic memory profile
                if (segments.Count == 0)
                {
                    // Placeholder segment for single region
                    // We handle single region logic below, but unify for calculation
                    totalDumpBytes = (long)profiles.Memory.Length * profiles.DumpCount;
                }
                else
                {
                    long singlePassBytes = segments.Sum(s => (long)s.Size);
                    try
                    {
                        totalDumpBytes = checked(singlePassBytes * profiles.DumpCount);
                    }
                    catch (OverflowException ex)
                    {
                        throw new InvalidOperationException(
                            $"Total dump size calculation overflowed (Single pass size={singlePassBytes}, Dump count={profiles.DumpCount}).",
                            ex);
                    }
                }

                long globalBytesRead = 0;
                DateTime dumpStartTime = _timeProvider.GetUtcNow();

                // Dumper payload is installed ONCE. We can invoke it multiple times.

                for (int iter = 0; iter < profiles.DumpCount; iter++)
                {
                    effectiveTaskLogger.LogInformation("Starting Dump Iteration {Iter}/{Total}", iter + 1, profiles.DumpCount);

                    if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
                    {
                        var selectedSegments = profiles.MemoryMapping.SelectedSegments.ToList();
                        List<byte[]> segmentDataList = [];

                        for (int i = 0; i < selectedSegments.Count; i++)
                        {
                            MemorySegment segment = selectedSegments[i];
                            uint segmentStart = uint.Parse(segment.StartAddress.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                            uint segmentSize = (uint)segment.Size;

                            string stageName = $"Dumping Seg {i + 1}/{selectedSegments.Count} (Iter {iter + 1}/{profiles.DumpCount})";

                            // Calculate start % for this specific segment
                            // Base is 20%. Range is 75%.
                            // percent = 20 + (75 * globalBytesRead / totalDumpBytes)
                            double currentBasePercent = 20.0 + (75.0 * globalBytesRead / totalDumpBytes);

                            progress.Report((stageName, currentBasePercent, globalBytesRead, totalDumpBytes));

                            effectiveTaskLogger.LogInformation("Dumping segment {Index}/{Total} (Iter {Iter}): '{Name}'",
                                i + 1, selectedSegments.Count, iter + 1, segment.Name);

                            int lastLoggedPercent = -1;
                            double lastReportedPercent = currentBasePercent;

                            var segmentProgress = new Progress<long>(bytesRead =>
                            {
                                long totalReadSoFar = globalBytesRead + bytesRead;
                                double percent = 20.0 + (75.0 * totalReadSoFar / totalDumpBytes);

                                // Report if changed by >= 0.1% or complete
                                if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesRead == segmentSize)
                                {
                                    progress.Report((stageName, percent, totalReadSoFar, totalDumpBytes));
                                    lastReportedPercent = percent;
                                }

                                // Log occasionally
                                if (segmentSize > 0)
                                {
                                    double segPct = (double)bytesRead / segmentSize * 100.0;
                                    if ((int)segPct > lastLoggedPercent && (int)segPct % 5 == 0) // Log every 5%
                                    {
                                        lastLoggedPercent = (int)segPct;
                                        effectiveTaskLogger.LogDebug("  Progress: {Percent:F1}% ({Bytes:N0}/{Total:N0})", segPct, bytesRead, segmentSize);
                                    }
                                }
                            });

                            byte[] segmentData = await client.InvokeDumperAsync(
                                segmentStart,
                                segmentSize,
                                segmentProgress,
                                cancellationToken).ConfigureAwait(false);

                            segmentDataList.Add(segmentData);
                            globalBytesRead += segmentData.Length;

                            effectiveTaskLogger.LogInformation("  ✓ Segment dumped: {Size:N0} bytes", segmentData.Length);
                        }

                        // Concatenate segments for this iteration
                        allDumps.Add([.. segmentDataList.SelectMany(arr => arr)]);
                    }
                    else
                    {
                        // Single Region Dump
                        string stageName = $"Dumping Memory (Iter {iter + 1}/{profiles.DumpCount})";

                        double currentBasePercent = 20.0 + (75.0 * globalBytesRead / totalDumpBytes);

                        progress.Report((stageName, currentBasePercent, globalBytesRead, totalDumpBytes));

                        effectiveTaskLogger.LogInformation("Dumping single region (Iter {Iter}/{Total})", iter + 1, profiles.DumpCount);

                        int lastLoggedPercent = -1;
                        double lastReportedPercent = currentBasePercent;

                        var dumpProgress = new Progress<long>(bytesRead =>
                        {
                            long totalReadSoFar = globalBytesRead + bytesRead;
                            double percent = 20.0 + (75.0 * totalReadSoFar / totalDumpBytes);

                            if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesRead == profiles.Memory.Length)
                            {
                                progress.Report((stageName, percent, totalReadSoFar, totalDumpBytes));
                                lastReportedPercent = percent;
                            }

                            if (profiles.Memory.Length > 0)
                            {
                                double dumpPct = (double)bytesRead / profiles.Memory.Length * 100.0;
                                if ((int)dumpPct > lastLoggedPercent && (int)dumpPct % 5 == 0)
                                {
                                    lastLoggedPercent = (int)dumpPct;
                                    effectiveTaskLogger.LogDebug("  Progress: {Percent:F1}%", dumpPct);
                                }
                            }
                        });


                        byte[] data = await client.InvokeDumperAsync(
                            profiles.Memory.Start,
                            profiles.Memory.Length,
                            dumpProgress,
                            cancellationToken).ConfigureAwait(false);

                        allDumps.Add(data);
                        globalBytesRead += data.Length;

                        effectiveTaskLogger.LogInformation("  ✓ Iteration {Iter} complete: {Size:N0} bytes", iter + 1, data.Length);
                    }
                }

                TimeSpan dumpDuration = _timeProvider.GetUtcNow() - dumpStartTime;
                long totalBytesDumped = allDumps.Sum(d => d.Length);
                double transferRate = totalBytesDumped > 0 && dumpDuration.TotalSeconds > 0 ? totalBytesDumped / dumpDuration.TotalSeconds : 0;

                effectiveTaskLogger.LogInformation("✓ All dumps completed: {Size:N0} bytes total", totalBytesDumped);
                effectiveTaskLogger.LogInformation("  Total Duration: {Duration:F1}s, Avg Rate: {Rate:F1} bytes/s",
                    dumpDuration.TotalSeconds, transferRate);

                // Stage 12: Teardown (95% progress)
                progress.Report(("teardown", 95.0, null, null));
                effectiveTaskLogger.LogInformation("--- Stage 12: Teardown ---");

                // Client will be disposed automatically via 'await using'

                // Stage 13: Complete (100% progress)
                progress.Report(("complete", 100.0, null, null));
                effectiveTaskLogger.LogInformation("=== BOOTLOADER DUMP OPERATION COMPLETED ===");

                return allDumps;
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
