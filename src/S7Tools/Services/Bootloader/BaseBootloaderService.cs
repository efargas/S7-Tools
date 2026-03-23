using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Bootloader;

/// <summary>
/// shared base class for bootloader services containing common logic like memory dumping.
/// </summary>
public abstract class BaseBootloaderService
{
    private readonly ITimeProvider _timeProvider;

    protected BaseBootloaderService(ITimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider!;
    }

    /// <summary>
    /// Gets the delay in milliseconds to wait between segment dumps within a single iteration.
    /// Override in derived classes to provide a configurable value from application settings.
    /// </summary>
    protected virtual int SegmentDumpDelayMilliseconds => 5000;

    /// <summary>
    /// Gets the delay in milliseconds to wait between dump iterations.
    /// Override in derived classes to provide a configurable value from application settings.
    /// </summary>
    protected virtual int IterationDumpDelayMilliseconds => 5000;

    /// <summary>
    /// Performs the core memory dump process, iterating through dumps and segments.
    /// </summary>
    protected async Task<List<byte[]>> PerformDumpProcessAsync(
        IPlcClient client,
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        ILogger logger,
        double startPercent,
        double weight,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(logger);

        List<byte[]> allDumps = [];

        long totalDumpBytes;
        var segments = profiles.MemoryMapping?.SelectedSegments?.ToList() ?? [];

        if (profiles.DumpCount <= 0)
        {
            throw new InvalidOperationException($"Invalid dump count: {profiles.DumpCount}. Must be >= 1.");
        }

        try
        {
            if (segments.Count == 0)
            {
                totalDumpBytes = checked((long)profiles.Memory.Length * profiles.DumpCount);
            }
            else
            {
                long singlePassBytes = segments.Sum(s => (long)s.Size);
                totalDumpBytes = checked(singlePassBytes * profiles.DumpCount);
            }
        }
        catch (OverflowException ex)
        {
            throw new InvalidOperationException(
                $"Total dump size calculation overflowed (DumpCount={profiles.DumpCount}).",
                ex);
        }

        if (totalDumpBytes <= 0)
        {
            totalDumpBytes = 1;
        }

        long globalBytesRead = 0;
        DateTime dumpStartTime = _timeProvider?.GetUtcNow() ?? DateTime.UtcNow;

        // Pre-calculate data for efficiency
        string[] iterStageNames = new string[profiles.DumpCount];
        string[][]? segStageNames = null;
        List<MemorySegment>? selectedSegments = null;

        if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
        {
            selectedSegments = profiles.MemoryMapping.SelectedSegments.ToList();
            segStageNames = new string[profiles.DumpCount][];
            for (int iter = 0; iter < profiles.DumpCount; iter++)
            {
                segStageNames[iter] = new string[selectedSegments.Count];
                string prefix = "Dumping Seg ";
                string suffix = $"/{selectedSegments.Count} (Iter {iter + 1}/{profiles.DumpCount})";
                for (int i = 0; i < selectedSegments.Count; i++)
                {
                    segStageNames[iter][i] = $"{prefix}{i + 1}{suffix}";
                }
            }
        }
        else
        {
            for (int iter = 0; iter < profiles.DumpCount; iter++)
            {
                iterStageNames[iter] = $"Dumping Memory (Iter {iter + 1}/{profiles.DumpCount})";
            }
        }

        for (int iter = 0; iter < profiles.DumpCount; iter++)
        {
            logger.LogInformation("Starting Dump Iteration {Iter}/{Total}", iter + 1, profiles.DumpCount);

            if (selectedSegments != null && segStageNames != null)
            {
                List<byte[]> segmentDataList = [];

                int segmentDumpDelayMilliseconds = SegmentDumpDelayMilliseconds;

                for (int i = 0; i < selectedSegments.Count; i++)
                {
                    if (i > 0 && segmentDumpDelayMilliseconds > 0)
                    {
                        var delay = TimeSpan.FromMilliseconds(segmentDumpDelayMilliseconds);
                        logger.LogDebug("Waiting {Delay} before next segment dump...", delay);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }

                    MemorySegment segment = selectedSegments[i];

                    string startStr = segment.StartAddress;
                    if (string.IsNullOrEmpty(startStr))
                    {
                        throw new InvalidOperationException("Memory segment start address is null.");
                    }

                    if (startStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        startStr = startStr[2..];
                    }

                    if (!uint.TryParse(startStr, System.Globalization.NumberStyles.HexNumber, null, out uint segmentStart))
                    {
                        throw new InvalidOperationException($"Invalid memory segment start address '{segment.StartAddress}'.");
                    }

                    uint segmentSize = (uint)segment.Size;
                    string stageName = segStageNames[iter][i];

                    double currentBasePercent = startPercent + (weight * globalBytesRead / totalDumpBytes);

                    progress.Report((stageName, currentBasePercent, globalBytesRead, totalDumpBytes));

                    logger.LogInformation("Dumping segment {Index}/{Total} (Iter {Iter}): '{Name}'",
                        i + 1, selectedSegments.Count, iter + 1, segment.Name);

                    int lastLoggedPercent = -1;
                    double lastReportedPercent = currentBasePercent;

                    var segmentProgress = new Progress<long>(bytesRead =>
                    {
                        long totalReadSoFar = globalBytesRead + bytesRead;
                        double percent = startPercent + (weight * totalReadSoFar / totalDumpBytes);

                        if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesRead == segmentSize)
                        {
                            progress.Report((stageName, percent, totalReadSoFar, totalDumpBytes));
                            lastReportedPercent = percent;
                        }

                        if (segmentSize > 0)
                        {
                            double segPct = (double)bytesRead / segmentSize * 100.0;
                            if ((int)segPct > lastLoggedPercent && (int)segPct % 5 == 0)
                            {
                                lastLoggedPercent = (int)segPct;
                                logger.LogDebug("  Progress: {Percent:F1}% ({Bytes:N0}/{Total:N0})", segPct, bytesRead, segmentSize);
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

                    logger.LogInformation("  ✓ Segment dumped: {Size:N0} bytes", segmentData.Length);
                }

                // High-performance flattening using Buffer.BlockCopy
                int totalIterSize = segmentDataList.Sum(s => s.Length);
                byte[] flattenedData = new byte[totalIterSize];
                int currentPos = 0;
                foreach (byte[] segData in segmentDataList)
                {
                    Buffer.BlockCopy(segData, 0, flattenedData, currentPos, segData.Length);
                    currentPos += segData.Length;
                }
                allDumps.Add(flattenedData);
            }
            else
            {
                string stageName = iterStageNames[iter];

                double currentBasePercent = startPercent + (weight * globalBytesRead / totalDumpBytes);
                progress.Report((stageName, currentBasePercent, globalBytesRead, totalDumpBytes));

                logger.LogInformation("Dumping single region (Iter {Iter}/{Total})", iter + 1, profiles.DumpCount);

                int lastLoggedPercent = -1;
                double lastReportedPercent = currentBasePercent;

                var dumpProgress = new Progress<long>(bytesRead =>
                {
                    long totalReadSoFar = globalBytesRead + bytesRead;
                    double percent = startPercent + (weight * totalReadSoFar / totalDumpBytes);

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
                            logger.LogDebug("  Progress: {Percent:F1}%", dumpPct);
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

                logger.LogInformation("  ✓ Iteration {Iter} complete: {Size:N0} bytes", iter + 1, data.Length);
            }

            if (iter < profiles.DumpCount - 1)
            {
                int iterationDelayMs = IterationDumpDelayMilliseconds;
                if (iterationDelayMs > 0)
                {
                    logger.LogDebug("Waiting {DelayMs}ms before next dump iteration...", iterationDelayMs);
                    await Task.Delay(iterationDelayMs, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        TimeSpan dumpDuration = (_timeProvider?.GetUtcNow() ?? DateTime.UtcNow) - dumpStartTime;
        long totalBytesDumped = allDumps.Sum(d => d.Length);
        double transferRate = totalBytesDumped > 0 && dumpDuration.TotalSeconds > 0 ? totalBytesDumped / dumpDuration.TotalSeconds : 0;

        logger.LogInformation("✓ All dumps completed: {Size:N0} bytes total", totalBytesDumped);
        logger.LogInformation("  Total Duration: {Duration:F1}s, Avg Rate: {Rate:F1} bytes/s",
            dumpDuration.TotalSeconds, transferRate);

        return allDumps;
    }

    /// <summary>
    /// Performs memory dump using streaming, writing directly to the final output file to minimize memory usage.
    /// Handles both segmented and single-region dumps.
    /// </summary>
    protected async Task<BootloaderResult> PerformDumpProcessStreamingAsync(
        IPlcClient client,
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        ILogger logger,
        double startPercent,
        double weight,
        Guid? taskId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(logger);

        List<string> savedFiles = [];

        try
        {
            DateTime dumpStartTime = _timeProvider?.GetUtcNow() ?? DateTime.UtcNow;
            int iterationCount = profiles.DumpCount > 0 ? profiles.DumpCount : 1;

            logger.LogInformation("Starting streaming dump process ({Count} iterations)", iterationCount);

            // Calculate total expected bytes across all iterations for progress reporting
            long totalExpectedBytes = 0;
            var segments = profiles.MemoryMapping?.SelectedSegments?.ToList() ?? [];
            if (segments.Count > 0)
            {
                totalExpectedBytes = iterationCount * segments.Sum(s => (long)s.Size);
            }
            else
            {
                totalExpectedBytes = iterationCount * (long)profiles.Memory.Length;
            }

            // Determine directory and sanitized job name once
            string dumpsDir = !string.IsNullOrWhiteSpace(profiles.OutputPath)
                ? profiles.OutputPath
                : "./dumps";

            if (!System.IO.Directory.Exists(dumpsDir))
            {
                System.IO.Directory.CreateDirectory(dumpsDir);
            }

            string rawJobName = segments.FirstOrDefault()?.Name ?? "MemoryDump";
            string jobName = string.Join("_", rawJobName.Split(System.IO.Path.GetInvalidFileNameChars()));
            string taskIdStr = taskId.HasValue ? $"_{taskId.Value:N}" : "";

            for (int iter = 0; iter < iterationCount; iter++)
            {
                if (iter > 0)
                {
                    logger.LogInformation("Waiting 5 seconds before next dump iteration...");
                    await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                }

                logger.LogInformation("Iteration {Iter}/{Total}", iter + 1, iterationCount);

                // Determine final file path up-front
                string timestamp = (_timeProvider?.GetLocalNow() ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
                string dumpFileName = $"{jobName}_iter{iter + 1}_of_{iterationCount}{taskIdStr}_{timestamp}.bin";
                string finalFilePath = System.IO.Path.Combine(dumpsDir, dumpFileName);
                long bytesWrittenInIter = 0;

                // Pass context object to helper methods
                var context = new StreamingContext(
                    client,
                    progress,
                    logger,
                    startPercent,
                    weight,
                    iterationCount,
                    iter,
                    totalExpectedBytes,
                    cancellationToken
                );

                if (segments.Count > 0)
                {
                    bytesWrittenInIter = await StreamSegmentedDumpToFileAsync(
                        context,
                        segments,
                        finalFilePath).ConfigureAwait(false);
                }
                else
                {
                    bytesWrittenInIter = await StreamSingleRegionDumpToFileAsync(
                        context,
                        profiles.Memory,
                        finalFilePath).ConfigureAwait(false);
                }

                // The data is now saved to the file at finalFilePath.
                // We no longer populate the deprecated `allDumps` list with empty arrays.
                // Consumers should rely on `savedFiles` for data access.
                savedFiles.Add(finalFilePath);
                logger.LogInformation("✓ Dump file created: {File} ({Size:N0} bytes)", dumpFileName, bytesWrittenInIter);
            }

            TimeSpan dumpDuration = (_timeProvider?.GetUtcNow() ?? DateTime.UtcNow) - dumpStartTime;
            long totalBytes = savedFiles.Sum(path => new System.IO.FileInfo(path).Length);
            double rate = totalBytes > 0 && dumpDuration.TotalSeconds > 0 ? totalBytes / dumpDuration.TotalSeconds : 0;

            logger.LogInformation("✓ Streaming dump complete: {Size:N0} bytes total", totalBytes);
            logger.LogInformation("  Duration: {Duration:F1}s, Rate: {Rate:F1} bytes/s", dumpDuration.TotalSeconds, rate);

            return new BootloaderResult(savedFiles);
        }
        finally
        {
            await client.StopDumperSessionAsync().ConfigureAwait(false);
        }
    }

    private record StreamingContext(
        IPlcClient Client,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> Progress,
        ILogger Logger,
        double StartPercent,
        double Weight,
        int IterationCount,
        int CurrentIteration,
        long TotalExpectedBytes,
        CancellationToken CancellationToken);

    private static uint ParseSegmentAddress(MemorySegment segment)
    {
        string? startStr = segment.StartAddress;
        if (string.IsNullOrEmpty(startStr))
        {
            throw new InvalidOperationException($"Memory segment '{segment.Name}' has a null or empty start address.");
        }

        if (startStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            startStr = startStr[2..];
        }

        if (!uint.TryParse(startStr, System.Globalization.NumberStyles.HexNumber, null, out uint segmentStart))
        {
            throw new InvalidOperationException($"Invalid memory segment start address '{segment.StartAddress}'.");
        }
        return segmentStart;
    }

    private async Task<long> StreamSegmentedDumpToFileAsync(
        StreamingContext ctx,
        List<MemorySegment> segments,
        string finalFilePath)
    {
        long bytesWrittenInIter = 0;
        long totalSegmentsSize = segments.Sum(s => (long)s.Size);

        // Pre-calculate data for efficiency
        string[] stageNames = new string[segments.Count];
        long[] segmentOffsets = new long[segments.Count];
        long currentOffset = 0;
        string prefix = "Seg ";
        string suffix = $"/{segments.Count} (Iter {ctx.CurrentIteration + 1}/{ctx.IterationCount})";

        for (int i = 0; i < segments.Count; i++)
        {
            stageNames[i] = $"{prefix}{i + 1}{suffix}";
            segmentOffsets[i] = currentOffset;
            currentOffset += segments[i].Size;
        }

        await using (var fileStream = new System.IO.FileStream(
            finalFilePath,
            System.IO.FileMode.Create,
            System.IO.FileAccess.ReadWrite,
            System.IO.FileShare.None,
            81920,
            true))
        {
            for (int i = 0; i < segments.Count; i++)
            {
                if (i > 0)
                {
                    ctx.Logger.LogInformation("Waiting 5 seconds before next segment dump...");
                    await Task.Delay(5000, ctx.CancellationToken).ConfigureAwait(false);
                }

                var segment = segments[i];
                uint segStart = ParseSegmentAddress(segment);
                uint segLength = (uint)segment.Size;
                string stageName = stageNames[i];
                long bytesFromPreviousSegmentsThisIter = segmentOffsets[i];

                if (segLength == 0)
                {
                    ctx.Logger.LogWarning("Skipping zero-length segment {Name}", segment.Name);

                    // Report progress for the skipped segment to avoid UI stalls.
                    long bytesFromPreviousIterations = ctx.CurrentIteration * totalSegmentsSize;
                    long cumulativeTotalBytes = bytesFromPreviousIterations + bytesFromPreviousSegmentsThisIter;
                    double percent = ctx.TotalExpectedBytes > 0
                        ? ctx.StartPercent + (ctx.Weight * cumulativeTotalBytes / ctx.TotalExpectedBytes)
                        : ctx.StartPercent;

                    ctx.Progress.Report((stageName, percent, cumulativeTotalBytes, ctx.TotalExpectedBytes));
                    continue;
                }

                double segWeight = ctx.Weight / ctx.IterationCount / segments.Count;
                double segStartPercent = ctx.StartPercent + (ctx.Weight * (ctx.CurrentIteration * segments.Count + i) / (ctx.IterationCount * segments.Count));

                ctx.Logger.LogInformation("  Streaming segment {Index}: {Name} (0x{Addr:X8}, {Size:N0} bytes)",
                    i + 1, segment.Name, segStart, segLength);

                long segBytesWrittenLocal = 0;
                double lastReportedSegPercent = segStartPercent;

                var segProgress = new Progress<long>(bytes =>
                {
                    segBytesWrittenLocal = bytes;
                    double percent = segStartPercent + (segWeight * bytes / segLength);

                    // Calculate cumulative bytes
                    long bytesFromPreviousIterations = ctx.CurrentIteration * totalSegmentsSize;
                    long cumulativeTotalBytes = bytesFromPreviousIterations + bytesFromPreviousSegmentsThisIter + bytes;

                    double threshold = segLength > 1024 * 1024 ? 0.1 : 1.0;

                    if (Math.Abs(percent - lastReportedSegPercent) >= threshold || bytes == segLength)
                    {
                        ctx.Progress.Report((stageName, percent, cumulativeTotalBytes, ctx.TotalExpectedBytes));
                        lastReportedSegPercent = percent;
                    }
                });

                await ctx.Client.InvokeDumperStreamAsync(
                    segStart, segLength,
                    async data => await fileStream.WriteAsync(data, ctx.CancellationToken),
                    segProgress,
                    ctx.CancellationToken,
                    logger: ctx.Logger).ConfigureAwait(false);

                // Use the local variable that was updated by the progress callback
                // or fall back to segLength if the callback didn't fire for some reason
                long bytesCompleted = segBytesWrittenLocal > 0 ? segBytesWrittenLocal : segLength;
                bytesWrittenInIter += bytesCompleted;

                ctx.Logger.LogDebug("  ✓ Segment {Index} streamed: {Size:N0} bytes", i + 1, bytesCompleted);
            }

            await fileStream.FlushAsync(ctx.CancellationToken).ConfigureAwait(false);

            // Trim if needed
            if (fileStream.Length > totalSegmentsSize)
            {
                ctx.Logger.LogDebug("Trimmed dump from {Original} to {Expected} bytes", fileStream.Length, totalSegmentsSize);
                fileStream.SetLength(totalSegmentsSize);
            }
        }

        return bytesWrittenInIter;
    }

    private async Task<long> StreamSingleRegionDumpToFileAsync(
        StreamingContext ctx,
        MemoryRegionProfile memoryRegion,
        string finalFilePath)
    {
        long bytesWrittenInIter = 0;
        uint segStart = memoryRegion.Start;
        uint segLength = (uint)memoryRegion.Length;

        if (segLength == 0)
        {
            ctx.Logger.LogWarning("Skipping zero-length memory region");
            return 0;
        }

        await using (var fileStream = new System.IO.FileStream(
            finalFilePath,
            System.IO.FileMode.Create,
            System.IO.FileAccess.ReadWrite,
            System.IO.FileShare.None,
            81920,
            true))
        {
            string stageName = $"Memory Dump (Iter {ctx.CurrentIteration + 1}/{ctx.IterationCount})";
            double iterWeight = ctx.Weight / ctx.IterationCount;
            double iterStartPercent = ctx.StartPercent + (ctx.Weight * ctx.CurrentIteration / ctx.IterationCount);

            ctx.Logger.LogInformation("  Streaming memory: 0x{Start:X8}, {Length:N0} bytes", segStart, segLength);

            double lastReportedPercent = iterStartPercent;

            var regionProgress = new Progress<long>(bytes =>
            {
                bytesWrittenInIter = bytes;
                double percent = iterStartPercent + (iterWeight * bytes / segLength);
                long cumulativeBytes = (ctx.CurrentIteration * segLength) + bytes;

                double threshold = segLength > 1024 * 1024 ? 0.1 : 1.0;
                if (Math.Abs(percent - lastReportedPercent) >= threshold || bytes == segLength)
                {
                    ctx.Progress.Report((stageName, percent, cumulativeBytes, ctx.TotalExpectedBytes));
                    lastReportedPercent = percent;
                }
            });

            await ctx.Client.InvokeDumperStreamAsync(
                segStart, segLength,
                async data => await fileStream.WriteAsync(data, ctx.CancellationToken),
                regionProgress,
                ctx.CancellationToken,
                logger: ctx.Logger).ConfigureAwait(false);

            await fileStream.FlushAsync(ctx.CancellationToken).ConfigureAwait(false);

            // Trim if needed
            long expectedSize = (long)memoryRegion.Length;
            if (fileStream.Length > expectedSize)
            {
                ctx.Logger.LogDebug("Trimmed dump from {Original} to {Expected} bytes", fileStream.Length, expectedSize);
                fileStream.SetLength(expectedSize);
            }
        }

        return bytesWrittenInIter;
    }

    /// <summary>
    /// Helper to report progress while waiting for a delay.
    /// </summary>
    protected async Task WaitWithProgressAsync(
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

    /// <summary>
    /// Performs the complete bootloader orchestration (13 stages) using streaming for dumps.
    /// This centralizes the logic previously duplicated in BootloaderService and EnhancedBootloaderService.
    /// </summary>
    protected async Task<BootloaderResult> PerformBootloaderOrchestrationAsync(
        JobProfileSet profiles,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        ILogger effectiveTaskLogger,
        ILogger? processLogger,
        ISerialPortService serialPort,
        ISocatService socat,
        IPowerSupplyService power,
        IPayloadProvider payloads,
        Func<JobProfileSet, IPlcClient> clientFactory,
        CancellationToken cancellationToken,
        Guid? taskId = null)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(serialPort);
        ArgumentNullException.ThrowIfNull(socat);
        ArgumentNullException.ThrowIfNull(power);
        ArgumentNullException.ThrowIfNull(payloads);
        ArgumentNullException.ThrowIfNull(clientFactory);

        const int InitialPowerOffWaitMs = 10000;
        SocatProcessInfo? socatProcess = null;
        bool isPowerConnected = false;

        try
        {
            // Stage 0: Configure serial port (1% progress)
            progress.Report(("serial_config", 1.0, null, null));
            effectiveTaskLogger.LogDebug("Configuring serial port {Device} with profile configuration", profiles.Serial.Device);

            bool serialConfigured = await serialPort.ApplyConfigurationAsync(
                profiles.Serial.Device,
                profiles.Serial.Configuration,
                effectiveTaskLogger,
                cancellationToken).ConfigureAwait(false);

            if (!serialConfigured)
            {
                throw new InvalidOperationException($"Failed to configure serial port {profiles.Serial.Device}");
            }

            effectiveTaskLogger.LogInformation("✓ Serial port {Device} configured successfully", profiles.Serial.Device);

            // Stage 1: Setup socat bridge (3% progress)
            progress.Report(("socat_setup", 3.0, null, null));
            effectiveTaskLogger.LogDebug("Setting up socat bridge on port {Port}", profiles.Socat.Port);

            if (profiles.Socat.Configuration == null)
            {
                throw new InvalidOperationException("Socat configuration is required but was null.");
            }

            profiles.Socat.Configuration.BaudRate = profiles.Serial.Baud;

            socatProcess = await socat.StartSocatAsync(
                profiles.Socat.Configuration,
                profiles.Serial.Device,
                processLogger,
                cancellationToken).ConfigureAwait(false);

            effectiveTaskLogger.LogInformation("✓ Socat bridge started on TCP port {Port} (PID: {ProcessId})",
                profiles.Socat.Port, socatProcess.ProcessId);

            // Stage 2: Connect to power supply (5% progress)
            progress.Report(("power_connect", 5.0, null, null));
            effectiveTaskLogger.LogDebug("Connecting to power supply at {Host}:{Port}", profiles.Power.Host, profiles.Power.Port);

            if (profiles.Power.Configuration == null)
            {
                throw new InvalidOperationException("Power supply configuration is required but was null.");
            }

            bool connected = await power.ConnectAsync(profiles.Power.Configuration, effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
            if (!connected)
            {
                throw new InvalidOperationException("Failed to connect to power supply.");
            }
            isPowerConnected = true;

            effectiveTaskLogger.LogInformation("✓ Connected to power supply at {Host}:{Port}", profiles.Power.Host, profiles.Power.Port);

            // Stage 3: Initial Power OFF (6% progress)
            progress.Report(("power_off_initial", 6.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 3: Initial Power OFF ---");

            bool powerOff = await power.TurnOffAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
            if (!powerOff)
            {
                throw new InvalidOperationException("Failed to turn PLC power OFF");
            }

            await WaitWithProgressAsync(
                InitialPowerOffWaitMs,
                progress,
                6.0, 10.0,
                "power_off_wait",
                cancellationToken).ConfigureAwait(false);

            effectiveTaskLogger.LogInformation("✓ PLC powered OFF and wait time completed");

            // Stage 4: Power ON (10% progress)
            progress.Report(("power_on", 10.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 4: Power ON PLC ---");

            bool powerOn = await power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
            if (!powerOn)
            {
                throw new InvalidOperationException("Failed to turn PLC power ON");
            }

            effectiveTaskLogger.LogInformation("✓ PLC powered ON");

            // Stage 5: Wait for stabilization (10% -> 11%)
            await WaitWithProgressAsync(
                profiles.PowerOnTimeMs,
                progress,
                10.0, 11.0,
                "power_on_stabilize",
                cancellationToken).ConfigureAwait(false);

            // Stage 6: PLC Client & Connect (12% progress)
            progress.Report(("plc_connect", 12.0, null, null));
            await using IPlcClient client = clientFactory(profiles);

            effectiveTaskLogger.LogDebug("Connecting PLC client to localhost:{Port}", profiles.Socat.Port);
            await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

            // Stage 7: Power Cycle (13% -> 15%)
            progress.Report(("power_cycle", 13.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 7: Power Cycle PLC ---");

            await power.TurnOffAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);

            await WaitWithProgressAsync(
                profiles.PowerOffDelayMs,
                progress,
                13.0, 15.0,
                "power_cycle_wait",
                cancellationToken).ConfigureAwait(false);

            await power.TurnOnAsync(effectiveTaskLogger, cancellationToken).ConfigureAwait(false);
            effectiveTaskLogger.LogInformation("✓ PLC power cycled successfully");

            await client.HandshakeAsync(cancellationToken).ConfigureAwait(false);

            // Stage 8: Handshake Info (15% progress)
            progress.Report(("handshake", 15.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 8: Bootloader Handshake ---");

            string version = await client.GetBootloaderVersionAsync(cancellationToken).ConfigureAwait(false);
            effectiveTaskLogger.LogInformation("✓ Connected to bootloader version: {Version}", version);

            // Stage 9: Install Stager (16% progress)
            progress.Report(("stager_install", 16.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 9: Install Stager Payload ---");

            byte[] stagerPayload = await payloads.GetStagerAsync(profiles.Payloads.BasePath, cancellationToken).ConfigureAwait(false);

            using (var stagerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var progressTask = SimulateProgressAsync(
                    stagerPayload.LongLength,
                    profiles.Serial.Configuration.BaudRate,
                    progress,
                    16.0, 18.0,
                    "stager_install",
                    isStagerInstall: true,
                    stagerCts.Token);

                try
                {
                    await client.InstallStagerAsync(stagerPayload, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    stagerCts.Cancel();
                    try
                    { await progressTask.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                }
            }
            effectiveTaskLogger.LogInformation("✓ Stager payload installed successfully ({Size} bytes)", stagerPayload.Length);

            // Stage 10: Install Dumper (18% progress)
            progress.Report(("dumper_install", 18.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 10: Install Memory Dumper Payload ---");

            byte[] dumperPayload = await payloads.GetMemoryDumperAsync(profiles.Payloads.BasePath, cancellationToken).ConfigureAwait(false);

            using (var dumperCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var progressTask = SimulateProgressAsync(
                    dumperPayload.LongLength,
                    profiles.Serial.Configuration.BaudRate,
                    progress,
                    18.0, 20.0,
                    "dumper_install",
                    isStagerInstall: false,
                    dumperCts.Token);

                try
                {
                    await client.InstallDumperAsync(dumperPayload, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    dumperCts.Cancel();
                    try
                    { await progressTask.ConfigureAwait(false); }
                    catch (OperationCanceledException) { }
                }
            }
            effectiveTaskLogger.LogInformation("✓ Dumper payload installed successfully");

            // Stage 11: Memory Dump (20% - 95% progress) - 75% weight
            effectiveTaskLogger.LogInformation("--- Stage 11: Memory Dump (Streaming) ---");

            var dumpResult = await PerformDumpProcessStreamingAsync(
                client, profiles,
                progress, effectiveTaskLogger,
                startPercent: 20.0, weight: 75.0,
                taskId,
                cancellationToken).ConfigureAwait(false);

            // Stage 12: Teardown (95% progress)
            progress.Report(("teardown", 95.0, null, null));
            effectiveTaskLogger.LogInformation("--- Stage 12: Teardown ---");

            // Stage 13: Complete
            progress.Report(("complete", 100.0, null, null));
            effectiveTaskLogger.LogInformation("=== BOOTLOADER DUMP OPERATION COMPLETED ===");

            return dumpResult;
        }
        catch (Exception ex)
        {
            processLogger?.LogError(ex, "Dump failed: {ErrorMessage}", ex.Message);
            effectiveTaskLogger.LogError("DUMP OPERATION FAILED: {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            if (socatProcess != null)
            {
                try
                {
                    await socat.StopSocatAsync(socatProcess, cancellationToken).ConfigureAwait(false);
                    effectiveTaskLogger.LogDebug("✓ Socat process stopped");
                }
                catch (Exception ex) { effectiveTaskLogger.LogWarning(ex, "Failed to stop socat"); }
            }

            if (isPowerConnected)
            {
                try
                {
                    await power.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    effectiveTaskLogger.LogDebug("✓ Disconnected from power supply");
                }
                catch (Exception ex) { effectiveTaskLogger.LogWarning(ex, "Failed to disconnect power"); }
            }
        }
    }

    /// <summary>
    /// Simulates progress for payload transfer with ACCURATE protocol overhead calculation.
    /// Accounts for: packet framing (length+checksum), chunking, and protocol-specific delays.
    /// </summary>
    /// <param name="payloadSize">Raw payload size in bytes</param>
    /// <param name="baudRate">UART baud rate (bits per second)</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="startPercent">Starting progress percentage</param>
    /// <param name="targetPercent">Target progress percentage</param>
    /// <param name="stage">Stage name for progress reporting</param>
    /// <param name="isStagerInstall">True for stager (uses IRAM write protocol), false for dumper (uses stager protocol)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected async Task SimulateProgressAsync(
        long payloadSize,
        int baudRate,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        double startPercent,
        double targetPercent,
        string stage,
        bool isStagerInstall,
        CancellationToken cancellationToken)
    {
        const int bitsPerByte = 10;
        long totalWireBytes;
        int totalDelayMs;

        if (isStagerInstall)
        {
            const int chunkSize = 16;
            int numChunks = (int)Math.Ceiling((double)payloadSize / chunkSize);
            totalWireBytes = 10 + (numChunks * 36) + 5 + 16;

            int totalPackets = (numChunks * 2) + 4;
            int avgPacketSize = (int)(totalWireBytes / totalPackets);
            int chunksPerPacket = (avgPacketSize + 1) / 2;
            totalDelayMs = totalPackets * (10 + (chunksPerPacket * 10));
        }
        else
        {
            const int maxPacketPayload = 64;
            int numPackets = (int)Math.Ceiling((double)payloadSize / maxPacketPayload);

            int avgPayloadPerPacket = (int)((payloadSize + numPackets - 1) / numPackets);
            totalWireBytes = numPackets * (1 + avgPayloadPerPacket + 1);

            int avgChunksPerPacket = (avgPayloadPerPacket + 2 + 1) / 2;
            totalDelayMs = numPackets * (10 + (avgChunksPerPacket * 10));
        }

        double transferTimeMs = ((double)totalWireBytes * bitsPerByte * 1000.0) / (double)baudRate;
        int totalTimeMs = (int)(transferTimeMs + totalDelayMs);
        totalTimeMs = (int)(totalTimeMs * 1.1);

        if (totalTimeMs < 1000)
        {
            totalTimeMs = 1000;
        }

        await WaitWithProgressAsync(
            totalTimeMs,
            progress,
            startPercent,
            targetPercent,
            stage,
            cancellationToken).ConfigureAwait(false);
    }
}
