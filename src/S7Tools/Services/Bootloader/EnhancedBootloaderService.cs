using System.Linq;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Validation;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Extensions;
using S7Tools.Resources;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// Consolidated bootloader service orchestrating complete memory dump workflow with TaskExecution integration.
/// Provides retry mechanisms, comprehensive error handling, and detailed progress tracking.
/// </summary>
public sealed class EnhancedBootloaderService(
    ILogger<EnhancedBootloaderService> logger,
    IPayloadProvider payloads,
    ISocatService socat,
    IPowerSupplyService power,
    ISerialPortService serialPort,
    Func<JobProfileSet, IPlcClient> clientFactory,
    IResourceCoordinator resourceCoordinator)
    : BaseBootloaderService(null), IEnhancedBootloaderService, IDisposable
{
    private readonly ILogger<EnhancedBootloaderService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPayloadProvider _payloads = payloads ?? throw new ArgumentNullException(nameof(payloads));
    private readonly ISocatService _socat = socat ?? throw new ArgumentNullException(nameof(socat));
    private readonly IPowerSupplyService _power = power ?? throw new ArgumentNullException(nameof(power));
    private readonly ISerialPortService _serialPort = serialPort ?? throw new ArgumentNullException(nameof(serialPort));
    private readonly Func<JobProfileSet, IPlcClient> _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    private readonly IResourceCoordinator _resourceCoordinator = resourceCoordinator ?? throw new ArgumentNullException(nameof(resourceCoordinator));
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
    private RetryConfiguration _retryConfiguration = RetryConfiguration.Default;
    private bool _disposed;

    public async Task<BootloaderResult> DumpAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        Microsoft.Extensions.Logging.ILogger? processLogger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);

        Microsoft.Extensions.Logging.ILogger effectiveTaskLogger = taskLogger ?? _logger;

        effectiveTaskLogger.LogInformation("Starting enhanced bootloader dump operation (delegating to base orchestration)");

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
    public RetryConfiguration RetryConfiguration => _retryConfiguration;

    /// <inheritdoc />
    public void UpdateRetryConfiguration(RetryConfiguration configuration)
    {
        _retryConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc />
    public async Task<BootloaderResult> DumpWithTaskTrackingAsync(
        TaskExecution taskExecution,
        JobProfileSet profiles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(taskExecution);
        ArgumentNullException.ThrowIfNull(profiles);

        return await _operationSemaphore.ExecuteAsync(async () =>
        {
            _logger.LogInformation("Starting enhanced bootloader dump operation for task {TaskId}", taskExecution.TaskId);

            // Update task to running state
            taskExecution.UpdateState(TaskState.Running, "Initializing bootloader operation");

            // Create a progress reporter that updates the TaskExecution
            double lastLoggedPercent = -1.0;
            var progressReporter = new Progress<(string stage, double percent, long? bytesRead, long? totalBytes)>(progress =>
            {
                (string? stage, double percent, long? bytesRead, long? totalBytes) = progress;
                // Progress is already in 0-100 range from BootloaderService
                string operation = GetUserFriendlyOperationName(stage);

                var extraData = new Dictionary<string, object>();
                if (bytesRead.HasValue && totalBytes.HasValue)
                {
                    extraData["BytesRead"] = bytesRead.Value;
                    extraData["TotalBytes"] = totalBytes.Value;
                }

                taskExecution.UpdateProgress(percent, operation, extraData);

                // Throttle logging to avoid spam (log every 0.1% change or if bytes are involved/important stages)
                if (Math.Abs(percent - lastLoggedPercent) >= 0.1 || percent >= 100.0 || percent <= 0.0)
                {
                    lastLoggedPercent = percent;
                    if (bytesRead.HasValue && totalBytes.HasValue)
                    {
                        _logger.LogDebug("Task {TaskId} progress: {Percentage:F1}% - {Operation} ({BytesRead}/{TotalBytes} bytes)",
                            taskExecution.TaskId, percent, operation, bytesRead, totalBytes);
                    }
                    else
                    {
                        _logger.LogDebug("Task {TaskId} progress: {Percentage:F1}% - {Operation}",
                            taskExecution.TaskId, percent, operation);
                    }
                }
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
                Microsoft.Extensions.Logging.ILogger? taskLogger = taskExecution.Logger?.MainLogger;
                Microsoft.Extensions.Logging.ILogger? processLogger = taskExecution.Logger?.ProcessLogger;

                // Execute the memory dump with retry logic
                // Execute the memory dump with retry logic
                // Directly call Orchestration to get both data and file paths
                var result = await ExecuteWithRetryAsync(
                    () => PerformBootloaderOrchestrationAsync(
                        profiles,
                        progressReporter,
                        taskLogger ?? _logger,
                        processLogger,
                        _serialPort,
                        _socat,
                        _power,
                        _payloads,
                        _clientFactory,
                        cancellationToken,
                        taskExecution.TaskId),
                    RetryableOperations.All,
                    taskExecution,
                    cancellationToken).ConfigureAwait(false);

                // No need to save manually, Orchestration handled it.
                // Output paths are in available in result.SavedFiles
                string outputFilePath = result.SavedFiles?.FirstOrDefault() ?? string.Empty;

                // Mark task as completed
                long totalLength = result.SavedFiles.Sum(x => (long)x.Length);
                taskExecution.MarkAsCompleted(outputFilePath, totalLength);

                _logger.LogInformation("Enhanced bootloader dump completed successfully for task {TaskId}. " +
                    "Output saved to: {OutputPath}", taskExecution.TaskId, outputFilePath);

                return result;
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
        }, cancellationToken);
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
                : ValidationResult.Failure([.. validationErrors.Select(error =>
                    new ValidationError("Resource", error))]);

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
                AvailableMemoryRegions =
                [
                    new()
                    {
                        Name = "Flash Memory",
                        StartAddress = profiles.Memory.Start,
                        Size = profiles.Memory.Length,
                        AccessFlags = MemoryAccessFlags.Read,
                        Description = "Main flash memory region"
                    }
                ]
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
            const double TransferRateBytesPerSecond = 1024.0;
            const double FixedOverheadSeconds = 30.0; // Setup, handshake, teardown

            double transferTimeSeconds = profiles.Memory.Length / TransferRateBytesPerSecond;
            double totalTimeSeconds = transferTimeSeconds + FixedOverheadSeconds;

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
            "power_off_initial" => "Initial Power OFF",
            "power_cycle" => "Power cycling PLC",
            "handshake" => "Establishing bootloader connection",
            "stager_install" => "Installing bootloader stager",
            "memory_dump" => "Dumping memory",
            "teardown" => "Cleaning up resources",
            "complete" => "Operation complete",
            _ => stage.Replace("_", " ")
        };
    }




    private static ResourceKey[] ExtractResourceKeys(JobProfileSet profiles)
    {
        return
        [
            new ResourceKey("serial", profiles.Serial.Device),
            new ResourceKey("tcp", profiles.Socat.Port.ToString()),
            new ResourceKey("modbus", $"{profiles.Power.Host}:{profiles.Power.Port}")
        ];
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
