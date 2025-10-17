using Microsoft.Extensions.Logging;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Validation;
using System.Linq;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// Enhanced bootloader service with TaskExecution integration, retry mechanisms, and comprehensive error handling.
/// Extends the basic bootloader functionality with detailed progress tracking and resilient operation patterns.
/// </summary>
public sealed class EnhancedBootloaderService : IEnhancedBootloaderService, IDisposable
{
    private readonly ILogger<EnhancedBootloaderService> _logger;
    private readonly IBootloaderService _baseBootloaderService;
    private readonly IResourceCoordinator _resourceCoordinator;
    private readonly IValidationService _validationService;
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

    private RetryConfiguration _retryConfiguration = RetryConfiguration.Default;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedBootloaderService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="baseBootloaderService">The base bootloader service to wrap.</param>
    /// <param name="resourceCoordinator">Service for resource coordination and conflict detection.</param>
    /// <param name="validationService">Service for validation operations.</param>
    public EnhancedBootloaderService(
        ILogger<EnhancedBootloaderService> logger,
        IBootloaderService baseBootloaderService,
        IResourceCoordinator resourceCoordinator,
        IValidationService validationService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseBootloaderService = baseBootloaderService ?? throw new ArgumentNullException(nameof(baseBootloaderService));
        _resourceCoordinator = resourceCoordinator ?? throw new ArgumentNullException(nameof(resourceCoordinator));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    }

    /// <inheritdoc />
    public RetryConfiguration RetryConfiguration => _retryConfiguration;

    /// <inheritdoc />
    public void UpdateRetryConfiguration(RetryConfiguration configuration)
    {
        _retryConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("Bootloader retry configuration updated: {Configuration}",
            new {
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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);

        _logger.LogInformation("Starting basic bootloader dump operation for job profiles");

        return await _baseBootloaderService.DumpAsync(profiles, progress, cancellationToken)
            .ConfigureAwait(false);
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
                // Execute the memory dump with retry logic
                byte[] memoryData = await ExecuteWithRetryAsync(
                    () => _baseBootloaderService.DumpAsync(profiles, progressReporter, cancellationToken),
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

            // Additional profile validation
            ValidationResult profileValidation = await _validationService.ValidateAsync(profiles, cancellationToken)
                .ConfigureAwait(false);

            if (!profileValidation.IsValid)
            {
                validationErrors.AddRange(profileValidation.Errors.Select(e => e.ErrorMessage));
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
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
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
