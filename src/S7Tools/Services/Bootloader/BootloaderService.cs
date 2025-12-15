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
        Microsoft.Extensions.Logging.ILogger? processLogger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);

        _logger.LogInformation("Starting bootloader dump operation");

        SocatProcessInfo? socatProcess = null;

        try
        {
            // Stage 0: Configure serial port (2% progress)
            progress.Report(("serial_config", 0.02));
            _logger.LogDebug("Configuring serial port {Device} with profile configuration", profiles.Serial.Device);

            // Use serial configuration directly from profile
            bool serialConfigured = await _serialPort.ApplyConfigurationAsync(
                profiles.Serial.Device,
                profiles.Serial.Configuration,
                cancellationToken).ConfigureAwait(false);

            if (!serialConfigured)
            {
                throw new InvalidOperationException($"Failed to configure serial port {profiles.Serial.Device}");
            }

            _logger.LogInformation("Serial port {Device} configured successfully", profiles.Serial.Device);
            processLogger?.LogInformation("Serial port configured: {Device} @ {Baud} baud",
                profiles.Serial.Device, profiles.Serial.Baud);

            // Stage 1: Setup socat bridge (5% progress)
            progress.Report(("socat_setup", 0.05));
            _logger.LogDebug("Setting up socat bridge on port {Port}", profiles.Socat.Port);

            // Use socat configuration directly from profile (must be non-null after Phase 2 changes)
            if (profiles.Socat.Configuration == null)
            {
                throw new InvalidOperationException("Socat configuration is required but was null. Ensure job profile includes full socat configuration.");
            }

            // Sync baud rate from serial profile to socat configuration to ensure correct speed
            profiles.Socat.Configuration.BaudRate = profiles.Serial.Baud;

            socatProcess = await _socat.StartSocatAsync(
                profiles.Socat.Configuration,
                profiles.Serial.Device,
                processLogger,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Socat bridge started on TCP port {Port}", profiles.Socat.Port);
            processLogger?.LogInformation("Socat bridge listening on localhost:{Port}", profiles.Socat.Port);

            // Stage 2: Connect to power supply (8% progress)
            progress.Report(("power_connect", 0.08));
            _logger.LogDebug("Connecting to power supply at {Host}:{Port}",
                profiles.Power.Host, profiles.Power.Port);

            // Use power configuration directly from profile (must be non-null after Phase 2 changes)
            if (profiles.Power.Configuration == null)
            {
                throw new InvalidOperationException("Power supply configuration is required but was null. Ensure job profile includes full power supply configuration.");
            }

            bool connected = await _power.ConnectAsync(profiles.Power.Configuration, cancellationToken).ConfigureAwait(false);
            if (!connected)
            {
                throw new InvalidOperationException(UIStrings.Exception_FailedToConnectToPowerSupply);
            }

            _logger.LogInformation("Connected to power supply at {Host}:{Port}",
                profiles.Power.Host, profiles.Power.Port);

            try
            {
                // Stage 3: Power ON PLC (10% progress)
                progress.Report(("power_on", 0.10));
                _logger.LogDebug("Turning PLC power ON");

                bool powerOn = await _power.TurnOnAsync(cancellationToken).ConfigureAwait(false);
                if (!powerOn)
                {
                    throw new InvalidOperationException("Failed to turn PLC power ON");
                }

                _logger.LogInformation("PLC powered ON");
                processLogger?.LogInformation("PLC power: ON");

                // Wait for initial power-on stabilization using PowerOnTimeMs from job profile
                _logger.LogDebug("Waiting {DelayMs}ms for PLC power stabilization", profiles.PowerOnTimeMs);
                await Task.Delay(profiles.PowerOnTimeMs, cancellationToken).ConfigureAwait(false);

                // Stage 4: Create PLC client and CONNECT to socat (12% progress)
                // We connect BEFORE power cycling to ensure the serial port is open and ready.
                // This eliminates the ~1-2s latency of socat/forking that causes us to miss the 500ms handshake window.
                progress.Report(("plc_connect", 0.12));
                await using IPlcClient client = _clientFactory(profiles);

                _logger.LogDebug("PLC client created. Establishing connection to socat TCP server...");
                processLogger?.LogInformation("Connecting PLC client to localhost:{Port}...", profiles.Socat.Port);

                await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("PLC client connected to socat (Ready for Handshake)");

                // Stage 5: Power cycle PLC (15% progress)
                progress.Report(("power_cycle", 0.15));
                _logger.LogDebug("Power cycling PLC: OFF → wait {PowerOffDelayMs}ms → ON", profiles.PowerOffDelayMs);

                // Power cycle: OFF → delay → ON (using PowerOffDelayMs from job profile)
                await _power.PowerCycleAsync(profiles.PowerOffDelayMs, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("PLC power cycled successfully (Client already connected)");
                processLogger?.LogInformation("PLC power cycle complete (OFF → {PowerOffDelayMs}ms → ON)",
                    profiles.PowerOffDelayMs);


                // Stage 6: Perform handshake (20% progress)
                progress.Report(("handshake", 0.20));
                _logger.LogDebug("Performing bootloader handshake");

                // client is already connected; HandshakeAsync will just perform the protocol handshake immediately.
                await client.HandshakeAsync(cancellationToken).ConfigureAwait(false);

                string version = await client.GetBootloaderVersionAsync(cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Connected to bootloader version: {Version}", version);
                processLogger?.LogInformation("Bootloader version: {Version}", version);

                // Stage 7: Install stager (30% progress)
                progress.Report(("stager_install", 0.30));
                _logger.LogDebug("Installing stager payload from {BasePath}", profiles.Payloads.BasePath);

                byte[] stagerPayload = await _payloads.GetStagerAsync(
                    profiles.Payloads.BasePath,
                    cancellationToken).ConfigureAwait(false);

                await client.InstallStagerAsync(stagerPayload, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogInformation("Stager payload installed successfully ({Size} bytes)", stagerPayload.Length);
                processLogger?.LogInformation("Stager installed: {Size} bytes", stagerPayload.Length);

                // Stage 8: Dump memory (50% - 95% progress)
                byte[] memoryData;

                if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
                {
                    // Multi-segment dump using MemoryMappingProfile
                    var selectedSegments = profiles.MemoryMapping.SelectedSegments.ToList();
                    _logger.LogInformation("Dumping {SegmentCount} selected memory segments from profile '{ProfileName}'",
                        selectedSegments.Count, profiles.MemoryMapping.Name);

                    var segmentDataList = new List<byte[]>();
                    long totalBytesRead = 0;
                    long totalSize = profiles.MemoryMapping.TotalSelectedSize;

                    byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                        profiles.Payloads.BasePath,
                        cancellationToken).ConfigureAwait(false);

                    for (int i = 0; i < selectedSegments.Count; i++)
                    {
                        MemorySegment segment = selectedSegments[i];
                        uint segmentStart = uint.Parse(segment.StartAddress.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                        uint segmentSize = (uint)segment.Size;

                        progress.Report(("memory_dump", 0.50 + (0.45 * totalBytesRead / totalSize)));
                        _logger.LogDebug("Dumping segment {Index}/{Total}: '{Name}' @ 0x{Address:X8} ({Size} bytes)",
                            i + 1, selectedSegments.Count, segment.Name, segmentStart, segmentSize);

                        var segmentProgress = new Progress<long>(bytesRead =>
                        {
                            double percent = 0.50 + (0.45 * (totalBytesRead + bytesRead) / totalSize);
                            progress.Report(("memory_dump", percent));
                        });

                        byte[] segmentData = await client.DumpMemoryAsync(
                            segmentStart,
                            segmentSize,
                            dumperPayload,
                            segmentProgress,
                            cancellationToken).ConfigureAwait(false);

                        segmentDataList.Add(segmentData);
                        totalBytesRead += segmentData.Length;

                        _logger.LogInformation("Segment '{Name}' dumped successfully: {Size} bytes", segment.Name, segmentData.Length);
                        processLogger?.LogInformation("Segment {Index}/{Total} '{Name}': {Size} bytes from 0x{Start:X8}",
                            i + 1, selectedSegments.Count, segment.Name, segmentData.Length, segmentStart);
                    }

                    // Concatenate all segment data
                    memoryData = segmentDataList.SelectMany(arr => arr).ToArray();
                    _logger.LogInformation("Multi-segment dump completed: {TotalSegments} segments, {TotalSize} bytes total",
                        selectedSegments.Count, memoryData.Length);
                }
                else
                {
                    // Single-region dump using legacy MemoryRegionProfile
                    progress.Report(("memory_dump", 0.50));
                    _logger.LogDebug("Dumping memory region 0x{Address:X8} - 0x{EndAddress:X8} ({Length} bytes)",
                        profiles.Memory.Start,
                        profiles.Memory.Start + profiles.Memory.Length,
                        profiles.Memory.Length);

                    byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                        profiles.Payloads.BasePath,
                        cancellationToken).ConfigureAwait(false);

                    var dumpProgress = new Progress<long>(bytesRead =>
                    {
                        double percent = 0.50 + (0.45 * bytesRead / profiles.Memory.Length);
                        progress.Report(("memory_dump", percent));
                    });

                    memoryData = await client.DumpMemoryAsync(
                        profiles.Memory.Start,
                        profiles.Memory.Length,
                        dumperPayload,
                        dumpProgress,
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Memory dump completed: {Size} bytes from 0x{Start:X8}",
                        memoryData.Length, profiles.Memory.Start);
                    processLogger?.LogInformation("Memory dump complete: {Size} bytes from 0x{Start:X8}",
                        memoryData.Length, profiles.Memory.Start);
                }

                // Stage 9: Teardown (95% progress)
                progress.Report(("teardown", 0.95));
                _logger.LogDebug("Cleaning up resources");

                // Client will be disposed automatically via 'await using'

                // Stage 10: Complete (100% progress)
                progress.Report(("complete", 1.0));
                _logger.LogInformation("Bootloader dump operation completed successfully. " +
                    "Dumped {ByteCount} bytes", memoryData.Length);

                return memoryData;
            }
            finally
            {
                // Always disconnect from power supply
                await _power.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Disconnected from power supply");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootloader dump operation failed: {ErrorMessage}", ex.Message);
            processLogger?.LogError("Dump failed: {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            if (socatProcess != null)
            {
                await _socat.StopSocatAsync(socatProcess, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Stopped socat process {PID}", socatProcess.ProcessId);
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
