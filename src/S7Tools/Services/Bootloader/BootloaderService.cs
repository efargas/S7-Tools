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


    public async Task<BootloaderResult> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        Microsoft.Extensions.Logging.ILogger? processLogger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);

        // Use taskLogger for task-level operations, fallback to main logger
        Microsoft.Extensions.Logging.ILogger effectiveTaskLogger = taskLogger ?? _logger;

        _logger.LogInformation("Starting bootloader dump operation (delegating to base orchestration)");

        return await PerformBootloaderOrchestrationAsync(
            profiles,
            progress,
            effectiveTaskLogger,
            processLogger,
            _serialPort,
            _socat,
            _power,
            _payloads,
            _clientFactory,
            cancellationToken).ConfigureAwait(false);
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
