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
    Func<JobProfileSet, IPlcClient> clientFactory) : BaseBootloaderService(timeProvider), IBootloaderService
{
    private readonly ILogger<BootloaderService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPayloadProvider _payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
    private readonly ISocatService _socat = socat ?? throw new ArgumentNullException(nameof(socat));
    private readonly IPowerSupplyService _power = power ?? throw new ArgumentNullException(nameof(power));
    private readonly ISerialPortService _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
    private readonly Func<JobProfileSet, IPlcClient> _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));


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

                // Stage 11: Memory Dump (20% - 95% progress) = 75% weight
                effectiveTaskLogger.LogInformation("--- Stage 11: Memory Dump ---");

                var allDumps = await PerformDumpProcessAsync(
                    client,
                    profiles,
                    progress,
                    effectiveTaskLogger,
                    20.0,
                    75.0,
                    cancellationToken).ConfigureAwait(false);

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
