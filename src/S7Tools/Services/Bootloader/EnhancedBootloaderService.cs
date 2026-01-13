using System.Linq;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Models.Validation;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Resources;
using S7Tools.Extensions;

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
    : IEnhancedBootloaderService, IDisposable
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

    private const int InitialPowerOffWaitMs = 10000;

    /// <inheritdoc />
    public RetryConfiguration RetryConfiguration => _retryConfiguration;

    /// <inheritdoc />
    public void UpdateRetryConfiguration(RetryConfiguration configuration)
    {
        _retryConfiguration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger.LogInformation("Bootloader retry configuration updated: {Configuration}",
            new
            {
                configuration.MaxConnectionRetries,
                configuration.MaxCommunicationRetries,
                configuration.MaxMemoryOperationRetries,
                configuration.InitialRetryDelay,
                configuration.UseExponentialBackoff
            });
    }

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

        Microsoft.Extensions.Logging.ILogger effectiveTaskLogger = taskLogger ?? _logger;

        effectiveTaskLogger.LogInformation("Starting enhanced bootloader dump operation");

        SocatProcessInfo? socatProcess = null;
        bool isPowerConnected = false;

        try
        {
            // Stage 0: Configure serial port (2% progress)
            progress.Report(("serial_config", 2.0, null, null));
            effectiveTaskLogger.LogDebug("Configuring serial port {Device} with profile configuration", profiles.Serial.Device);

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

            effectiveTaskLogger.LogInformation("Serial port {Device} configured successfully", profiles.Serial.Device);

            // Stage 1: Setup socat bridge (5% progress)
            progress.Report(("socat_setup", 5.0, null, null));
            effectiveTaskLogger.LogDebug("Setting up socat bridge on port {Port}", profiles.Socat.Port);

            // Use socat configuration directly from profile (must be non-null after Phase 2 changes)
            if (profiles.Socat.Configuration == null)
            {
                throw new InvalidOperationException("Socat configuration is required but was null. Ensure job profile includes full socat configuration.");
            }

            socatProcess = await _socat.StartSocatAsync(
                profiles.Socat.Configuration,
                profiles.Serial.Device,
                processLogger,
                protocolLogger,
                cancellationToken).ConfigureAwait(false);

            effectiveTaskLogger.LogInformation("Socat bridge started on TCP port {Port} (PID: {ProcessId})",
                profiles.Socat.Port, socatProcess.ProcessId);

            // Stage 2: Connect to power supply (8% progress)
            progress.Report(("power_connect", 8.0, null, null));
            _logger.LogDebug("Connecting to power supply at {Host}:{Port}",
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
            isPowerConnected = true;

            effectiveTaskLogger.LogInformation("Connected to power supply at {Host}:{Port}",
                profiles.Power.Host, profiles.Power.Port);

            // Match BootloaderService.cs logic for weighted progress
            // Stage 3: Power OFF PLC and wait (8% -> 9% progress)
            progress.Report(("power_off_initial", 9.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 3: Initial Power OFF ---");
            _logger.LogDebug("Turning PLC power OFF and waiting {WaitMs}ms", InitialPowerOffWaitMs);

            bool powerOff = await _power.TurnOffAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
            if (!powerOff)
            {
                throw new InvalidOperationException("Failed to turn PLC power OFF");
            }

            await WaitWithProgressAsync(
                InitialPowerOffWaitMs,
                progress,
                9.0, 15.0,
                "power_off_wait",
                cancellationToken).ConfigureAwait(false);

            effectiveTaskLogger.LogInformation("PLC powered OFF and wait time completed");

            // Stage 4: Power ON PLC (15% progress)
            progress.Report(("power_on", 15.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 4: Power ON PLC ---");
            effectiveTaskLogger.LogDebug("Turning PLC power ON");

            bool powerOn = await _power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
            if (!powerOn)
            {
                throw new InvalidOperationException("Failed to turn PLC power ON");
            }

            effectiveTaskLogger.LogInformation("PLC powered ON");
            processLogger?.LogInformation("PLC power: ON");

            // Stage 5: Wait for PLC to fully power on (15% -> 17%)
            effectiveTaskLogger.LogDebug("Waiting {DelayMs}ms for PLC to power on", profiles.PowerOnTimeMs);

            // FIX: Use granular wait for power on 
            await WaitWithProgressAsync(
                 profiles.PowerOnTimeMs,
                 progress,
                 15.0, 17.0,
                 "power_on_stabilize",
                 cancellationToken).ConfigureAwait(false);

            // Stage 6: Create PLC client and connect to socat (17% progress)
            progress.Report(("plc_connect", 17.0, null, null));
            await using IPlcClient client = _clientFactory(profiles);

            _logger.LogDebug("PLC client created and connecting to socat TCP server");
            processLogger?.LogInformation("Connecting PLC client to localhost:{Port}", profiles.Socat.Port);
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            // Stage 7: Power cycle PLC (20% -> 22% progress)
            progress.Report(("power_cycle", 20.0, null, null));
            effectiveTaskLogger.LogDebug("Power cycling PLC: OFF → wait {PowerOffDelayMs}ms → ON", profiles.PowerOffDelayMs);

            // Decomposed Power Cycle for progress reporting
            await _power.TurnOffAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);

            await WaitWithProgressAsync(
                profiles.PowerOffDelayMs, // Short wait
                progress,
                20.0, 22.0,
                "power_cycle_wait",
                cancellationToken).ConfigureAwait(false);

            await _power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);

            // Critical timing: Perform handshake immediately after power on
            await client.HandshakeAsync(cancellationToken).ConfigureAwait(false);

            effectiveTaskLogger.LogInformation("PLC power cycled successfully");

            // Stage 8: Handshake (22% progress) - Aligned with BootloaderService
            progress.Report(("handshake", 22.0, null, null));
            _logger.LogDebug("Performing bootloader handshake");

            string version = await client.GetBootloaderVersionAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Connected to bootloader version: {Version}", version);
            processLogger?.LogInformation("Bootloader version: {Version}", version);

            // Stage 9: Install stager (25% progress)
            progress.Report(("stager_install", 25.0, null, null));
            _logger.LogDebug("Installing stager payload from {BasePath}", profiles.Payloads.BasePath);
            byte[] stagerPayload = await _payloads.GetStagerAsync(
                profiles.Payloads.BasePath,
                cancellationToken).ConfigureAwait(false);

            // Simulate installation progress
            int baudRate = profiles.Serial.Configuration.BaudRate;
            using var stagerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task stagerProgressTask = SimulateProgressAsync(
                stagerPayload.Length,
                baudRate,
                progress,
                25.0, 28.0,
                "stager_install",
                stagerCts.Token);

            try
            {
                await client.InstallStagerAsync(stagerPayload, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                stagerCts.Cancel();
                try
                { await stagerProgressTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }

            effectiveTaskLogger.LogInformation("Stager payload installed successfully ({Size} bytes)", stagerPayload.Length);
            processLogger?.LogInformation("Stager installed: {Size} bytes", stagerPayload.Length);

            // Stage 10: Install Dumper Payload (28% progress)
            progress.Report(("dumper_install", 28.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 10: Install Memory Dumper Payload ---");

            byte[] dumperPayload = await _payloads.GetMemoryDumperAsync(
                profiles.Payloads.BasePath,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Memory dumper payload loaded: {Size} bytes", dumperPayload.Length);
            _logger.LogDebug("Installing dumper payload to PLC...");

            using var dumperCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task dumperProgressTask = SimulateProgressAsync(
                dumperPayload.Length,
                baudRate,
                progress,
                28.0, 30.0,
                "dumper_install",
                dumperCts.Token);

            try
            {
                await client.InstallDumperAsync(dumperPayload, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                dumperCts.Cancel();
                try
                { await dumperProgressTask.ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }

            effectiveTaskLogger.LogInformation("Dumper payload installed successfully");

            // Stage 11: Dump memory (20% - 95% progress) - 75% Weight
            effectiveTaskLogger.LogInformation("--- Stage 11: Memory Dump ---");

            // My logic shifted:
            // Pre-steps: 0-30%. 
            // Dump: 30-95% (65% weight).
            // Let's force alignment with BootloaderService logic of 20-95% (75% weight)
            // Effectively I just allocated 0-30% for pre-steps. 
            // That's acceptable, as long as the memory dump is the bulk. 
            // Let's use 30% -> 95% = 65% weight for this implementation to keep things smooth without jumping back.

            var allDumps = new List<byte[]>();

            // Calculate total bytes expected across ALL iterations and segments (for global progress)
            long totalDumpBytes = 0;
            var segments = profiles.MemoryMapping?.SelectedSegments?.ToList() ?? [];

            if (segments.Count == 0)
            {
                totalDumpBytes = (long)profiles.Memory.Length * profiles.DumpCount;
            }
            else
            {
                long singlePassBytes = segments.Sum(s => (long)s.Size);
                totalDumpBytes = singlePassBytes * profiles.DumpCount;
            }

            if (totalDumpBytes <= 0)
                totalDumpBytes = 1; // Prevent div/0
                
            long globalBytesRead = 0;

            for (int dumpIter = 0; dumpIter < profiles.DumpCount; dumpIter++)
            {
                effectiveTaskLogger.LogInformation("Starting Dump Iteration {Iter}/{Total}", dumpIter + 1, profiles.DumpCount);

                if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
                {
                    var selectedSegments = profiles.MemoryMapping.SelectedSegments.ToList();
                    List<byte[]> segmentDataList = [];

                    for (int i = 0; i < selectedSegments.Count; i++)
                    {
                        MemorySegment segment = selectedSegments[i];
                        string start = segment.StartAddress ?? throw new InvalidOperationException("Memory segment start address is null.");

                        if (start.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            start = start[2..];
                        }

                        if (!uint.TryParse(
                                start,
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out uint segmentStart))
                        {
                            throw new InvalidOperationException($"Invalid memory segment start address '{segment.StartAddress}'.");
                        }

                        uint segmentSize = (uint)segment.Size;

                        string stageName = $"Dumping Seg {i + 1}/{selectedSegments.Count} (Iter {dumpIter + 1}/{profiles.DumpCount})";

                        // 30% start, 65% range. 
                        double currentBasePercent = 30.0 + (65.0 * globalBytesRead / totalDumpBytes);
                        progress.Report((stageName, currentBasePercent, globalBytesRead, totalDumpBytes));

                        _logger.LogDebug("Dumping segment {Index}/{Total} (Iter {Iter}/{IterTotal}): '{Name}' @ 0x{Address:X8} ({Size} bytes)",
                            i + 1, selectedSegments.Count, dumpIter + 1, profiles.DumpCount, segment.Name, segmentStart, segmentSize);

                        bool isReading = false;
                        double lastReportedPercent = currentBasePercent;

                        var segmentProgress = new Progress<long>(bytesRead =>
                        {
                            if (!isReading)
                                isReading = true;

                            double percent = 30.0 + (65.0 * (globalBytesRead + bytesRead) / totalDumpBytes);

                            if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesRead == segmentSize)
                            {
                                progress.Report((stageName, percent, globalBytesRead + bytesRead, totalDumpBytes));
                                lastReportedPercent = percent;
                            }
                        });

                        byte[] segmentData;
                        try
                        {
                            segmentData = await client.InvokeDumperAsync(
                                segmentStart,
                                segmentSize,
                                segmentProgress,
                                cancellationToken).ConfigureAwait(false);
                        }
                        finally
                        {
                            // No upload task to clean up
                        }

                        segmentDataList.Add(segmentData);
                        globalBytesRead += segmentData.Length;

                        _logger.LogInformation("Segment '{Name}' (Iter {Iter}) dumped successfully: {Size} bytes",
                            segment.Name, dumpIter + 1, segmentData.Length);
                    }

                    allDumps.Add([.. segmentDataList.SelectMany(arr => arr)]);
                }
                else
                {
                    // Single-region dump
                    string stageName = $"Dumping Memory (Iter {dumpIter + 1}/{profiles.DumpCount})";
                    double currentBasePercent = 30.0 + (65.0 * globalBytesRead / totalDumpBytes);

                    progress.Report((stageName, currentBasePercent, globalBytesRead, totalDumpBytes));

                    _logger.LogDebug("Dumping memory region 0x{Address:X8} - 0x{EndAddress:X8} ({Length} bytes) (Iter {Iter}/{Total})",
                        profiles.Memory.Start,
                        profiles.Memory.Start + profiles.Memory.Length,
                        profiles.Memory.Length,
                        dumpIter + 1,
                        profiles.DumpCount);


                    bool isReading = false;
                    double lastReportedPercent = currentBasePercent;

                    var dumpProgress = new Progress<long>(bytesRead =>
                    {
                        if (!isReading)
                            isReading = true;
                        double percent = 30.0 + (65.0 * (globalBytesRead + bytesRead) / totalDumpBytes);

                        if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesRead == profiles.Memory.Length)
                        {
                            progress.Report((stageName, percent, globalBytesRead + bytesRead, totalDumpBytes));
                            lastReportedPercent = percent;
                        }
                    });

                    byte[] data;
                    try
                    {
                        data = await client.InvokeDumperAsync(
                            profiles.Memory.Start,
                            profiles.Memory.Length,
                            dumpProgress,
                            cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        // No upload task to clean up
                    }

                    allDumps.Add(data);
                    globalBytesRead += data.Length;
                }

                if (dumpIter < profiles.DumpCount - 1)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Memory dump completed. Total {Size} bytes.", globalBytesRead);

            // Stage 12: Teardown (95% progress)
            progress.Report(("teardown", 95.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 12: Teardown ---");
            _logger.LogDebug("Cleaning up resources");

            // Client will be disposed automatically via 'await using'

            // Stage 13: Complete (100% progress)
            progress.Report(("complete", 100.0, null, null));
            effectiveTaskLogger.LogInformation("=== BOOTLOADER DUMP OPERATION COMPLETED ===");
            _logger.LogInformation("Bootloader dump operation completed successfully. " +
                "Dumped {DumpCount} files", allDumps.Count);

            return allDumps;
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
    public async Task<IList<byte[]>> DumpWithTaskTrackingAsync(
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

                // Throttle logging to avoid spam (log every 1% change or if bytes are involved/important stages)
                if (Math.Abs(percent - lastLoggedPercent) >= 1.0 || percent >= 100.0 || percent <= 0.0)
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
                Microsoft.Extensions.Logging.ILogger? protocolLogger = taskExecution.Logger?.ProtocolLogger;

                // Execute the memory dump with retry logic
                IList<byte[]> memoryDataList = await ExecuteWithRetryAsync(
                    () => DumpAsync(profiles, progressReporter, taskLogger, processLogger, protocolLogger, cancellationToken),
                    RetryableOperations.All,
                    taskExecution,
                    cancellationToken).ConfigureAwait(false);

                // Save the output file(s)
                string outputFilePath = await SaveMemoryDumpAsync(
                    memoryDataList,
                    profiles.OutputPath,
                    taskExecution.TaskId,
                    cancellationToken).ConfigureAwait(false);

                // Mark task as completed
                long totalLength = memoryDataList.Sum(x => (long)x.Length);
                taskExecution.MarkAsCompleted(outputFilePath, totalLength);

                _logger.LogInformation("Enhanced bootloader dump completed successfully for task {TaskId}. " +
                    "Output saved to: {OutputPath}", taskExecution.TaskId, outputFilePath);

                return memoryDataList;
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

    private async Task<string> SaveMemoryDumpAsync(
        IList<byte[]> memoryDataList,
        string outputPath,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        if (memoryDataList == null || memoryDataList.Count == 0)
        {
            return string.Empty;
        }

        Directory.CreateDirectory(outputPath);

        // If only one dump, use standard naming
        if (memoryDataList.Count == 1)
        {
            string fileName = $"dump-{taskId:N}.bin";
            string fullPath = Path.Combine(outputPath, fileName);
            await File.WriteAllBytesAsync(fullPath, memoryDataList[0], cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Memory dump saved to: {FilePath} ({FileSize} bytes)",
                fullPath, memoryDataList[0].Length);

            return fullPath;
        }

        // Multi-dump: save files with iteration index
        string baseFileName = $"dump-{taskId:N}";
        var savedPaths = new List<string>();

        for (int i = 0; i < memoryDataList.Count; i++)
        {
            string fileName = $"{baseFileName}_iter{i + 1}.bin";
            string fullPath = Path.Combine(outputPath, fileName);
            await File.WriteAllBytesAsync(fullPath, memoryDataList[i], cancellationToken).ConfigureAwait(false);
            savedPaths.Add(fullPath);
        }

        _logger.LogInformation("Saved {Count} dump files to {BasePath}", memoryDataList.Count, outputPath);

        return savedPaths[0];
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

    private async Task SimulateProgressAsync(
        long payloadSize,
        int baudRate,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        double startPercent,
        double targetPercent,
        string stage,
        CancellationToken cancellationToken)
    {
        // 10 bits per byte (8 data + 1 start + 1 stop). Baud rate is bits/sec.
        // Duration in seconds = (size * 10) / baudRate
        // Ensure double arithmetic
        double durationSeconds = ((double)payloadSize * 10.0) / (double)baudRate;
        int delayMs = (int)(durationSeconds * 1000);

        // Add 10% buffering for overhead
        delayMs = (int)(delayMs * 1.1);

        // Force a minimum delay of 1 second to ensure the progress bar animation is visible to the user,
        // even for small payloads or high baud rates.
        if (delayMs < 1000)
        {
            delayMs = 1000;
        }

        await WaitWithProgressAsync(
            delayMs,
            progress,
            startPercent,
            targetPercent,
            stage,
            cancellationToken).ConfigureAwait(false);
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

        // Update every 100ms for smoother progress
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
            // Round to 1 decimal place to match UI display resolution and reduce noise
            currentPercent = Math.Round(currentPercent, 1);
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
