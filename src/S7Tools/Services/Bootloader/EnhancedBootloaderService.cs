using System.Linq;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Validation;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Resources;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// Consolidated bootloader service orchestrating complete memory dump workflow with TaskExecution integration.
/// Provides retry mechanisms, comprehensive error handling, and detailed progress tracking.
/// </summary>
public sealed class EnhancedBootloaderService : IEnhancedBootloaderService, IDisposable
{
    private readonly ILogger<EnhancedBootloaderService> _logger;
    private readonly IPayloadProvider _payloads;
    private readonly ISocatService _socat;
    private readonly IPowerSupplyService _power;
    private readonly ISerialPortService _serialPort;
    private readonly Func<JobProfileSet, IPlcClient> _clientFactory;
    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

    private RetryConfiguration _retryConfiguration = RetryConfiguration.Default;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedBootloaderService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="payloads">Payload provider for stager and dumper files.</param>
    /// <param name="socat">Socat service for serial-to-TCP bridge management.</param>
    /// <param name="power">Power supply service for PLC power control.</param>
    /// <param name="serialPort">Serial port service for device configuration.</param>
    /// <param name="clientFactory">Factory method for creating PLC client instances.</param>
    /// <param name="resourceCoordinator">Service for resource coordination and conflict detection.</param>
    public EnhancedBootloaderService(
        ILogger<EnhancedBootloaderService> logger,
        IPayloadProvider payloads,
        ISocatService socat,
        IPowerSupplyService power,
        ISerialPortService serialPort,
        Func<JobProfileSet, IPlcClient> clientFactory,
        IResourceCoordinator resourceCoordinator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
        _socat = socat ?? throw new ArgumentNullException(nameof(socat));
        _power = power ?? throw new ArgumentNullException(nameof(power));
        _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        _resourceCoordinator = resourceCoordinator ?? throw new ArgumentNullException(nameof(resourceCoordinator));
    }

    /// <inheritdoc />
    public RetryConfiguration RetryConfiguration => _retryConfiguration;

    /// <inheritdoc />
    public void UpdateRetryConfiguration(RetryConfiguration configuration)
    {
        _retryConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("Bootloader retry configuration updated: {Configuration}",
            new
            {
                MaxConnectionRetries = configuration.MaxConnectionRetries,
                MaxCommunicationRetries = configuration.MaxCommunicationRetries,
                MaxMemoryOperationRetries = configuration.MaxMemoryOperationRetries,
                InitialRetryDelay = configuration.InitialRetryDelay,
                UseExponentialBackoff = configuration.UseExponentialBackoff
            });
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
        bool isPowerConnected = false;

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

            socatProcess = await _socat.StartSocatAsync(
                profiles.Socat.Configuration,
                profiles.Serial.Device,
                processLogger,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Socat bridge started on TCP port {Port} (PID: {ProcessId})",
                profiles.Socat.Port, socatProcess.ProcessId);
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
            isPowerConnected = true;

            _logger.LogInformation("Connected to power supply at {Host}:{Port}",
                profiles.Power.Host, profiles.Power.Port);

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

            // Stage 4: Power cycle PLC (12% progress)
            progress.Report(("power_cycle", 0.12));
            _logger.LogDebug("Power cycling PLC: OFF → wait {PowerOffDelayMs}ms → ON", profiles.PowerOffDelayMs);

            // Power cycle: OFF → delay → ON (using PowerOffDelayMs from job profile)
            await _power.PowerCycleAsync(profiles.PowerOffDelayMs, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("PLC power cycled successfully");
            processLogger?.LogInformation("PLC power cycle complete (OFF → {PowerOffDelayMs}ms → ON)",
                profiles.PowerOffDelayMs);

            // Stage 5: Create PLC client and connect to socat (15% progress)
            progress.Report(("plc_connect", 0.15));
            await using IPlcClient client = _clientFactory(profiles);

            _logger.LogDebug("PLC client created and connecting to socat TCP server");
            processLogger?.LogInformation("Connecting PLC client to localhost:{Port}", profiles.Socat.Port);

            // Stage 6: Perform handshake (20% progress)
            progress.Report(("handshake", 0.20));
            _logger.LogDebug("Performing bootloader handshake");

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

                if (totalSize <= 0)
                {
                    throw new InvalidOperationException("Selected memory segments have a total size of 0 bytes.");
                }

                byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                    profiles.Payloads.BasePath,
                    cancellationToken).ConfigureAwait(false);

                for (int i = 0; i < selectedSegments.Count; i++)
                {
                    MemorySegment segment = selectedSegments[i];
                    string start = segment.StartAddress ?? throw new InvalidOperationException("Memory segment start address is null.");

                    if (start.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        start = start[2..];
                    }

                    if (!uint.TryParse(start, System.Globalization.NumberStyles.HexNumber, null, out uint segmentStart))
                    {
                        throw new InvalidOperationException($"Invalid memory segment start address '{segment.StartAddress}'.");
                    }

                    if (segment.Size <= 0)
                    {
                        throw new InvalidOperationException($"Invalid memory segment size '{segment.Size}' for segment '{segment.Name}'.");
                    }

                    uint segmentSize = checked((uint)segment.Size);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootloader dump operation failed: {ErrorMessage}", ex.Message);
            processLogger?.LogError("Dump failed: {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            // Always stop socat and disconnect from power supply
            if (socatProcess != null)
            {
                try
                {
                    await _socat.StopSocatAsync(socatProcess, cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug("Socat bridge on port {Port} stopped (PID: {ProcessId})",
                        profiles.Socat.Port, socatProcess.ProcessId);
                }
                catch (Exception teardownEx)
                {
                    _logger.LogWarning(teardownEx, "Failed to stop socat (PID: {ProcessId}) during teardown",
                        socatProcess.ProcessId);
                }
            }

            if (isPowerConnected)
            {
                try
                {
                    await _power.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    _logger.LogDebug("Disconnected from power supply");
                }
                catch (Exception teardownEx)
                {
                    _logger.LogWarning(teardownEx, "Failed to disconnect power supply during teardown");
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> DumpWithTaskTrackingAsync(
        TaskExecution taskExecution,
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskExecution);
        ArgumentNullException.ThrowIfNull(profiles);

        await _operationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _logger.LogInformation("Starting enhanced bootloader dump operation for task {TaskId}", taskExecution.TaskId);

            // Update task to running state
            taskExecution.UpdateState(TaskState.Running, "Initializing bootloader operation");

            // Create a progress reporter that updates the TaskExecution
            var progressReporter = new Progress<(string stage, double percent)>(progress =>
            {
                (string? stage, double percent) = progress;
                double progressPercentage = percent * 100.0;
                string operation = GetUserFriendlyOperationName(stage);

                taskExecution.UpdateProgress(progressPercentage, operation);

                _logger.LogDebug("Task {TaskId} progress: {Percentage:F1}% - {Operation}",
                    taskExecution.TaskId, progressPercentage, operation);
            });

            // Estimate operation time
            TimeSpan? estimatedTime = await EstimateOperationTimeAsync(profiles, cancellationToken)
                .ConfigureAwait(false);
            if (estimatedTime.HasValue)
            {
                taskExecution.EstimatedTimeRemaining = estimatedTime.Value;
            }

            try
            {
                // Get process logger from task execution if available
                Microsoft.Extensions.Logging.ILogger? processLogger = taskExecution.Logger?.ProcessLogger;

                // Execute the memory dump with retry logic
                byte[] memoryData = await ExecuteWithRetryAsync(
                    () => DumpAsync(profiles, progressReporter, processLogger, cancellationToken),
                    RetryableOperations.All,
                    taskExecution,
                    cancellationToken).ConfigureAwait(false);

                // Save the output file
                string outputFilePath = await SaveMemoryDumpAsync(
                    memoryData,
                    profiles.OutputPath,
                    taskExecution.TaskId,
                    cancellationToken).ConfigureAwait(false);

                // Mark task as completed
                taskExecution.MarkAsCompleted(outputFilePath, memoryData.Length);

                _logger.LogInformation("Enhanced bootloader dump completed successfully for task {TaskId}. " +
                    "Output saved to: {OutputPath}", taskExecution.TaskId, outputFilePath);

                return memoryData;
            }
            catch (OperationCanceledException)
            {
                taskExecution.UpdateState(TaskState.Cancelled, "Operation was cancelled");
                _logger.LogWarning("Bootloader dump operation cancelled for task {TaskId}", taskExecution.TaskId);
                throw;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Bootloader operation failed: {ex.Message}";
                taskExecution.MarkAsFailed(errorMessage, ex.ToString());

                _logger.LogError(ex, "Enhanced bootloader dump failed for task {TaskId}: {ErrorMessage}",
                    taskExecution.TaskId, ex.Message);

                throw new BootloaderOperationException(errorMessage, ex);
            }
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<ValidationResult> ValidateResourcesAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        _logger.LogDebug("Validating resources for bootloader operation");

        var validationErrors = new List<string>();

        try
        {
            // Validate resource availability through resource coordinator
            IEnumerable<ResourceKey> resourceKeys = ExtractResourceKeys(profiles);
            bool canAcquire = _resourceCoordinator.TryAcquire(resourceKeys);

            if (canAcquire)
            {
                // Release immediately since this is just a validation check
                _resourceCoordinator.Release(resourceKeys);
            }
            else
            {
                validationErrors.Add("One or more required resources are not available or are locked by another task");
            }

            // Additional profile validation using the built-in validation
            ValidationResult profileValidation = await ValidateProfileSetAsync(profiles, cancellationToken)
                .ConfigureAwait(false);

            if (!profileValidation.IsValid)
            {
                validationErrors.AddRange(profileValidation.Errors.Select(e => e.Message));
            }

            ValidationResult result = validationErrors.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.Failure(validationErrors.Select(error =>
                    new ValidationError("Resource", error)).ToArray());

            _logger.LogDebug("Resource validation completed. Valid: {IsValid}, Errors: {ErrorCount}",
                result.IsValid, validationErrors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resource validation failed: {ErrorMessage}", ex.Message);
            return ValidationResult.Failure("Resource", $"Resource validation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        _logger.LogDebug("Testing bootloader connection");

        try
        {
            // This would involve a lightweight connection test
            // For now, we'll simulate it by checking if resources are available
            ValidationResult validation = await ValidateResourcesAsync(profiles, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug("Connection test completed. Success: {Success}", validation.IsValid);
            return validation.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public Task<BootloaderInfo> GetBootloaderInfoAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        _logger.LogDebug("Retrieving bootloader information");

        try
        {
            // For now, return simulated bootloader info
            // In a real implementation, this would establish a connection and query the bootloader
            var bootloaderInfo = new BootloaderInfo
            {
                Version = "1.0.0",
                PlcModel = "S7-1200",
                FirmwareVersion = "V4.4",
                MaxTransferSize = 1024,
                SupportsPauseResume = false,
                Capabilities = BootloaderCapabilities.MemoryRead | BootloaderCapabilities.Checksums,
                AvailableMemoryRegions = new List<MemoryRegion>
                {
                    new MemoryRegion
                    {
                        Name = "Flash Memory",
                        StartAddress = profiles.Memory.Start,
                        Size = profiles.Memory.Length,
                        AccessFlags = MemoryAccessFlags.Read,
                        Description = "Main flash memory region"
                    }
                }
            };

            _logger.LogDebug("Retrieved bootloader info: Version={Version}, Model={Model}",
                bootloaderInfo.Version, bootloaderInfo.PlcModel);

            return Task.FromResult(bootloaderInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve bootloader information: {ErrorMessage}", ex.Message);
            throw new BootloaderOperationException($"Failed to retrieve bootloader information: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public Task<TimeSpan?> EstimateOperationTimeAsync(
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        try
        {
            // Simple estimation based on memory size
            // Assume ~1KB/second transfer rate plus fixed overhead
            const double transferRateBytesPerSecond = 1024.0;
            const double fixedOverheadSeconds = 30.0; // Setup, handshake, teardown

            double transferTimeSeconds = profiles.Memory.Length / transferRateBytesPerSecond;
            double totalTimeSeconds = transferTimeSeconds + fixedOverheadSeconds;

            var estimatedTime = TimeSpan.FromSeconds(totalTimeSeconds);

            _logger.LogDebug("Estimated operation time: {EstimatedTime} for {MemorySize} bytes",
                estimatedTime, profiles.Memory.Length);

            return Task.FromResult<TimeSpan?>(estimatedTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to estimate operation time: {ErrorMessage}", ex.Message);
            return Task.FromResult<TimeSpan?>(null);
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        RetryableOperations retryableOperation,
        TaskExecution taskExecution,
        CancellationToken cancellationToken)
    {
        if (!_retryConfiguration.RetryableOperations.HasFlag(retryableOperation))
        {
            return await operation().ConfigureAwait(false);
        }

        int maxRetries = GetMaxRetriesForOperation(retryableOperation);
        TimeSpan currentDelay = _retryConfiguration.InitialRetryDelay;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    taskExecution.UpdateProgress(
                        taskExecution.ProgressPercentage,
                        $"Retrying operation (attempt {attempt + 1}/{maxRetries + 1})");

                    _logger.LogInformation("Retrying bootloader operation for task {TaskId}, attempt {Attempt}/{MaxAttempts}",
                        taskExecution.TaskId, attempt + 1, maxRetries + 1);

                    await Task.Delay(currentDelay, cancellationToken).ConfigureAwait(false);
                }

                T? result = await operation().ConfigureAwait(false);

                if (attempt > 0)
                {
                    _logger.LogInformation("Bootloader operation succeeded for task {TaskId} on attempt {Attempt}",
                        taskExecution.TaskId, attempt + 1);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw; // Don't retry cancellation
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Bootloader operation failed for task {TaskId} on attempt {Attempt}, retrying: {ErrorMessage}",
                    taskExecution.TaskId, attempt + 1, ex.Message);

                // Calculate next delay with exponential backoff
                if (_retryConfiguration.UseExponentialBackoff)
                {
                    currentDelay = TimeSpan.FromMilliseconds(
                        Math.Min(
                            currentDelay.TotalMilliseconds * _retryConfiguration.BackoffMultiplier,
                            _retryConfiguration.MaxRetryDelay.TotalMilliseconds));
                }
            }
        }

        // If we get here, all retries have been exhausted
        throw new BootloaderOperationException($"Bootloader operation failed after {maxRetries + 1} attempts");
    }

    private int GetMaxRetriesForOperation(RetryableOperations operation)
    {
        return operation switch
        {
            RetryableOperations.Connection => _retryConfiguration.MaxConnectionRetries,
            RetryableOperations.Handshake => _retryConfiguration.MaxCommunicationRetries,
            RetryableOperations.PayloadInstallation => _retryConfiguration.MaxCommunicationRetries,
            RetryableOperations.MemoryRead => _retryConfiguration.MaxMemoryOperationRetries,
            RetryableOperations.PowerControl => _retryConfiguration.MaxConnectionRetries,
            RetryableOperations.Network => _retryConfiguration.MaxConnectionRetries,
            _ => _retryConfiguration.MaxCommunicationRetries
        };
    }

    private static string GetUserFriendlyOperationName(string stage)
    {
        return stage switch
        {
            "socat_setup" => "Setting up network bridge",
            "power_cycle" => "Power cycling PLC",
            "handshake" => "Establishing bootloader connection",
            "stager_install" => "Installing bootloader stager",
            "memory_dump" => "Dumping memory",
            "teardown" => "Cleaning up resources",
            "complete" => "Operation complete",
            _ => stage.Replace("_", " ")
        };
    }

    private async Task<string> SaveMemoryDumpAsync(
        byte[] memoryData,
        string outputPath,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure output directory exists
            Directory.CreateDirectory(outputPath);

            // Generate filename with timestamp and task ID
            string timestamp = DateTime.Now.ToString(DateTimeFormats.FileTimestamp);
            string fileName = $"memory_dump_{timestamp}_{taskId:N}.bin";
            string filePath = Path.Combine(outputPath, fileName);

            // Write memory data to file
            await File.WriteAllBytesAsync(filePath, memoryData, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Memory dump saved to: {FilePath} ({FileSize} bytes)",
                filePath, memoryData.Length);

            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save memory dump: {ErrorMessage}", ex.Message);
            throw new BootloaderOperationException($"Failed to save memory dump: {ex.Message}", ex);
        }
    }

    private static IEnumerable<ResourceKey> ExtractResourceKeys(JobProfileSet profiles)
    {
        return new[]
        {
            new ResourceKey("serial", profiles.Serial.Device),
            new ResourceKey("tcp", profiles.Socat.Port.ToString()),
            new ResourceKey("modbus", $"{profiles.Power.Host}:{profiles.Power.Port}")
        };
    }

    /// <summary>
    /// Releases the unmanaged resources used by the EnhancedBootloaderService and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _operationSemaphore?.Dispose();
            }
            _disposed = true;
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
                // This will throw OverflowException if Start + Length > uint.MaxValue
                _ = profiles.Memory.Start + profiles.Memory.Length;
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

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Exception thrown when bootloader operations fail.
/// </summary>
public class BootloaderOperationException : S7ToolsException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BootloaderOperationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BootloaderOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BootloaderOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
