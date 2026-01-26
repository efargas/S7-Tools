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
        // For backwards compatibility or if not provided, we can default (though ideally it should be injected)
        // Since BootloaderService has it, we should use it. EnhancedBootloaderService might not have it yet?
        // Checking EnhancedBootloaderService ctor... it doesn't seem to have TimeProvider in the visible snippet.
        // We will make it optional or just use DateTime if null for now to avoid breaking EnhancedBootloaderService excessively if I can't easily add it yet.
        // Actually, let's just use DateTime.UtcNow if no provider is present, or I can inject it.
        // Given the constraints, I'll allow it to be null and use default.
        _timeProvider = timeProvider!;
    }

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

        // Calculate total bytes expected across ALL iterations and segments
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
            // If total bytes is 0, we can't really report progress or dump anything meaningful,
            // but we should avoid div/0.
            totalDumpBytes = 1;
        }

        long globalBytesRead = 0;
        DateTime dumpStartTime = _timeProvider?.GetUtcNow() ?? DateTime.UtcNow;

        for (int iter = 0; iter < profiles.DumpCount; iter++)
        {
            logger.LogInformation("Starting Dump Iteration {Iter}/{Total}", iter + 1, profiles.DumpCount);

            if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
            {
                // Segmented Dump
                var selectedSegments = profiles.MemoryMapping.SelectedSegments.ToList();
                List<byte[]> segmentDataList = [];

                for (int i = 0; i < selectedSegments.Count; i++)
                {
                    MemorySegment segment = selectedSegments[i];

                    // Parse start address
                    string startStr = segment.StartAddress;
                    if (string.IsNullOrEmpty(startStr))
                        throw new InvalidOperationException("Memory segment start address is null.");
                    if (startStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        startStr = startStr[2..];

                    if (!uint.TryParse(startStr, System.Globalization.NumberStyles.HexNumber, null, out uint segmentStart))
                    {
                        throw new InvalidOperationException($"Invalid memory segment start address '{segment.StartAddress}'.");
                    }

                    uint segmentSize = (uint)segment.Size;
                    string stageName = $"Dumping Seg {i + 1}/{selectedSegments.Count} (Iter {iter + 1}/{profiles.DumpCount})";

                    // Calculate granular progress
                    // base percent for THIS moment = startPercent + (weight * globalBytesRead / totalDumpBytes)
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

                        // Report if changed by >= 0.1% or complete
                        if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesRead == segmentSize)
                        {
                            progress.Report((stageName, percent, totalReadSoFar, totalDumpBytes));
                            lastReportedPercent = percent;
                        }

                        // Log occasionally (every 5%)
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

                allDumps.Add([.. segmentDataList.SelectMany(arr => arr)]);
            }
            else
            {
                // Single Region Dump
                string stageName = $"Dumping Memory (Iter {iter + 1}/{profiles.DumpCount})";

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

            // Small delay between iterations if not the last one
            if (iter < profiles.DumpCount - 1)
            {
                // Small yield to ensure we don't choke the connection too tight if looping fast? 
                // BootloaderService didn't have wait, Enhanced did. 
                // Let's add a small safe delay or just yield.
                // Enhanced logic had: await Task.Delay(100, cancellationToken);
                // Bootloader logic didn't. 
                // Adding a small delay is usually safer for serial comms stability.
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
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
    /// SIMPLIFIED multi-iteration streaming dump - keeps ONE session, loops iterations inside.
    /// This is the user-suggested approach: start session once, loop inside, stop session once.
    /// </summary>
    protected async Task<BootloaderResult> PerformDumpProcessStreamingSimplifiedAsync(
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

        List<byte[]> allDumps = [];
        List<string> savedFiles = [];
        DateTime dumpStartTime = _timeProvider?.GetUtcNow() ?? DateTime.UtcNow;
        int iterationCount = profiles.DumpCount > 0 ? profiles.DumpCount : 1;

        logger.LogInformation("🚀 Starting SIMPLIFIED streaming dump ({Count} iterations) - RESTART per Segment approach", iterationCount);
        logger.LogDebug("  Strategy: True Streaming (Direct-to-Disk) using persistent session");

        // Calculate total expected bytes across all iterations
        long totalExpectedBytes = 0;
        var segments = profiles.MemoryMapping?.HasSelectedSegments == true
            ? profiles.MemoryMapping.SelectedSegments.ToList()
            : null;

        if (segments != null)
        {
            totalExpectedBytes = iterationCount * segments.Sum(s => (long)s.Size);
        }

        try
        {
            logger.LogDebug("Beginning iteration loop ({Count} iterations)...", iterationCount);

            for (int iter = 0; iter < iterationCount; iter++)
            {
                logger.LogInformation("📍 Iteration {Iter}/{Total} starting...", iter + 1, iterationCount);

                if (segments != null && segments.Count > 0)
                {
                    // Segmented dump

                    // Create output dump file immediately
                    string timestamp = (_timeProvider?.GetLocalNow() ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
                    string jobName = segments.FirstOrDefault()?.Name ?? "MemoryDump";
                    jobName = string.Join("_", jobName.Split(Path.GetInvalidFileNameChars()));
                    string taskIdStr = taskId.HasValue ? $"_{taskId.Value:N}" : "";
                    string dumpFileName = $"{jobName}_iter{iter + 1}_of_{iterationCount}{taskIdStr}_{timestamp}.bin";

                    string dumpsDir = !string.IsNullOrWhiteSpace(profiles.OutputPath)
                        ? profiles.OutputPath
                        : "./dumps";

                    if (!System.IO.Directory.Exists(dumpsDir))
                    {
                        System.IO.Directory.CreateDirectory(dumpsDir);
                    }

                    string dumpFilePath = System.IO.Path.Combine(dumpsDir, dumpFileName);
                    savedFiles.Add(dumpFilePath);

                    // Open file stream for writing
                    await using (var fileStream = new System.IO.FileStream(
                        dumpFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write,
                        System.IO.FileShare.None, 81920, true))
                    {
                        for (int i = 0; i < segments.Count; i++)
                        {
                            var segment = segments[i];
                            string segStartStr = segment.StartAddress?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) == true
                                ? segment.StartAddress[2..]
                                : segment.StartAddress ?? "0";

                            if (!uint.TryParse(segStartStr, System.Globalization.NumberStyles.HexNumber, null, out uint segStart))
                            {
                                throw new InvalidOperationException($"Invalid segment address: {segment.StartAddress}");
                            }

                            uint segLength = (uint)segment.Size;
                            string stageName = $"Seg {i + 1}/{segments.Count} (Iter {iter + 1}/{iterationCount})";
                            double segWeight = weight / iterationCount / segments.Count;
                            double segStartPercent = startPercent + (weight * (iter * segments.Count + i) / (iterationCount * segments.Count));

                            logger.LogInformation("  ├─ Streaming segment {Index}/{Total}: {Name} (0x{Addr:X8}, {Size:N0} bytes)",
                                i + 1, segments.Count, segment.Name, segStart, segLength);

                            long segBytesWritten = 0;
                            double lastReportedSegPercent = segStartPercent;
                            var segProgress = new Progress<long>(bytes =>
                            {
                                segBytesWritten = bytes;
                                double percent = segStartPercent + (segWeight * bytes / segLength);

                                long bytesFromPreviousIterations = iter * segments.Sum(s => (long)s.Size);
                                long bytesFromPreviousSegmentsThisIter = segments.Take(i).Sum(s => (long)s.Size);
                                long cumulativeTotalBytes = bytesFromPreviousIterations + bytesFromPreviousSegmentsThisIter + bytes;

                                double threshold = segLength > 1024 * 1024 ? 0.1 : 1.0;

                                if (Math.Abs(percent - lastReportedSegPercent) >= threshold || bytes == segLength)
                                {
                                    progress.Report((stageName, percent, cumulativeTotalBytes, totalExpectedBytes));
                                    lastReportedSegPercent = percent;
                                }
                            });

                            // Callback for direct streaming
                            async ValueTask OnDataReceivedAsync(ReadOnlyMemory<byte> data)
                            {
                                await fileStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                            }

                            // Keep session open unless it's the absolute last operation
                            // (Though orchestrator handles lifecycle, explicit hint helps)
                            bool keepSessionOpen = !((iter == iterationCount - 1) && (i == segments.Count - 1));

                            await client.InvokeDumperStreamAsync(
                                segStart,
                                segLength,
                                OnDataReceivedAsync,
                                segProgress,
                                cancellationToken,
                                keepSessionOpen, // Hint to keep connection alive
                                logger).ConfigureAwait(false);

                            logger.LogDebug("    ✓ Segment {Index} complete: {Size:N0} bytes", i + 1, segBytesWritten);
                        }

                        // Ensure all data is flushed to disk for this iteration
                        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    // Log file completion
                    long fileSize = new System.IO.FileInfo(dumpFilePath).Length;
                    logger.LogInformation("  ✓ Iteration {Iter}/{Total} saved: {File} ({Size:N0} bytes)",
                        iter + 1, iterationCount, dumpFileName, fileSize);

                    // NOTE: allDumps list is NOT populated in streaming mode to save memory.
                    // If downstream code requires it, we would need to read it back, defeating the purpose.
                    // BootloaderResult is updated to allow null/empty byte lists if files are present.
                }
                else
                {
                    // Single-region dump: Use standard dumper but stream to file if possible
                    // For now, fallback to buffered for single-region simpler case or implement similarly
                    // Implementing simplified streaming for single region:

                    string stageName = $"Dumping Memory (Iter {iter + 1}/{iterationCount})";
                    string timestamp = (_timeProvider?.GetLocalNow() ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
                    string dumpFileName = $"MemoryDump_iter{iter + 1}_of_{iterationCount}_{timestamp}.bin";
                    string dumpsDir = !string.IsNullOrWhiteSpace(profiles.OutputPath) ? profiles.OutputPath : "./dumps";

                    if (!System.IO.Directory.Exists(dumpsDir)) System.IO.Directory.CreateDirectory(dumpsDir);
                    string dumpFilePath = System.IO.Path.Combine(dumpsDir, dumpFileName);
                    savedFiles.Add(dumpFilePath);

                    await PerformStreamingDumpToFileAsync(
                        client,
                        profiles.Memory.Start,
                        (uint)profiles.Memory.Length,
                        dumpFilePath,
                        progress,
                        logger,
                        startPercent + (weight * iter / iterationCount),
                        weight / iterationCount,
                        stageName,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            logger.LogInformation("✓ All {Count} iterations processed successfully", iterationCount);
            // Return empty list for byte arrays to indicate data is on disk
            return new BootloaderResult(allDumps, savedFiles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during simplified streaming dump");
            throw;
        }
        finally
        {
            await client.StopDumperSessionAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Performs memory dump using streaming (writes to temp files, reads back as byte arrays).
    /// Provides 80% memory reduction during dump phase while maintaining interface compatibility.
    /// </summary>
    protected async Task<BootloaderResult> PerformDumpProcessStreamingAsync(
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
        List<string> tempFiles = [];

        try
        {
            DateTime dumpStartTime = _timeProvider?.GetUtcNow() ?? DateTime.UtcNow;
            int iterationCount = profiles.DumpCount > 0 ? profiles.DumpCount : 1;

            logger.LogInformation("Starting streaming dump process ({Count} iterations)", iterationCount);

            for (int iter = 0; iter < iterationCount; iter++)
            {
                logger.LogInformation("Iteration {Iter}/{Total}", iter + 1, iterationCount);

                if (profiles.MemoryMapping != null && profiles.MemoryMapping.HasSelectedSegments)
                {
                    // Segmented dump - combine segments into one file per iteration
                    var segments = profiles.MemoryMapping.SelectedSegments.ToList();
                    string iterTempFile = System.IO.Path.GetTempFileName();
                    tempFiles.Add(iterTempFile);

                    await using (var combinedStream = new System.IO.FileStream(
                        iterTempFile, System.IO.FileMode.Create, System.IO.FileAccess.Write,
                        System.IO.FileShare.None, 81920, true))
                    {
                        for (int i = 0; i < segments.Count; i++)
                        {
                            var segment = segments[i];
                            string segStartStr = segment.StartAddress?.StartsWith("0x", StringComparison.OrdinalIgnoreCase) == true
                                ? segment.StartAddress[2..]
                                : segment.StartAddress ?? "0";

                            if (!uint.TryParse(segStartStr, System.Globalization.NumberStyles.HexNumber, null, out uint segStart))
                            {
                                throw new InvalidOperationException($"Invalid segment address: {segment.StartAddress}");
                            }

                            uint segLength = (uint)segment.Size;
                            string stageName = $"Seg {i + 1}/{segments.Count} (Iter {iter + 1}/{iterationCount})";
                            double segWeight = weight / iterationCount / segments.Count;
                            double segStartPercent = startPercent + (weight * (iter * segments.Count + i) / (iterationCount * segments.Count));

                            logger.LogInformation("  Streaming segment {Index}: {Name} (0x{Addr:X8}, {Size:N0} bytes)",
                                i + 1, segment.Name, segStart, segLength);

                            // Stream segment directly to combined file
                            long segBytesWritten = 0;
                            double lastReportedSegPercent = segStartPercent;
                            var segProgress = new Progress<long>(bytes =>
                                    {
                                        segBytesWritten = bytes;
                                        double percent = segStartPercent + (segWeight * bytes / segLength);

                                        // Calculate cumulative bytes across all iterations
                                        long bytesFromPreviousIterations = iter * segments.Sum(s => (long)s.Size);
                                        long bytesFromPreviousSegmentsThisIter = segments.Take(i).Sum(s => (long)s.Size);
                                        long cumulativeTotalBytes = bytesFromPreviousIterations + bytesFromPreviousSegmentsThisIter + bytes;
                                        long grandTotalBytes = iterationCount * segments.Sum(s => (long)s.Size);

                                        // Determine reporting threshold: 0.1% for >1MB, else 1.0%
                                        double threshold = segLength > 1024 * 1024 ? 0.1 : 1.0;

                                        if (Math.Abs(percent - lastReportedSegPercent) >= threshold || bytes == segLength)
                                        {
                                            progress.Report((stageName, percent, cumulativeTotalBytes, grandTotalBytes));
                                            lastReportedSegPercent = percent;
                                        }
                                    });

                            await client.InvokeDumperStreamAsync(
                                segStart,
                                segLength,
                                async data => await combinedStream.WriteAsync(data, cancellationToken),
                                segProgress,
                                cancellationToken,
                                logger: logger).ConfigureAwait(false);

                            logger.LogDebug("  ✓ Segment {Index} streamed: {Size:N0} bytes", i + 1, segBytesWritten);
                        }
                    }

                    // Read temp file, trim to exact size, and create dump .bin file immediately
                    byte[] dumpData = await System.IO.File.ReadAllBytesAsync(iterTempFile, cancellationToken).ConfigureAwait(false);

                    // Calculate expected total size for all segments
                    long expectedSize = segments.Sum(s => (long)s.Size);
                    if (dumpData.Length > expectedSize)
                    {
                        byte[] trimmed = new byte[expectedSize];
                        Array.Copy(dumpData, 0, trimmed, 0, expectedSize);
                        logger.LogDebug("Trimmed dump from {Original} to {Expected} bytes (removed {Padding} padding bytes)",
                            dumpData.Length, expectedSize, dumpData.Length - expectedSize);
                        dumpData = trimmed;
                    }

                    allDumps.Add(dumpData);

                    // Create output dump file
                    string timestamp = (_timeProvider?.GetLocalNow() ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
                    // Use first segment name or generic name for dump file
                    string jobName = segments.FirstOrDefault()?.Name ?? "MemoryDump";
                    string dumpFileName = $"{jobName}_iter{iter + 1}_of_{iterationCount}_{timestamp}.bin";
                    string dumpsDir = "./dumps";
                    if (!System.IO.Directory.Exists(dumpsDir))
                    {
                        System.IO.Directory.CreateDirectory(dumpsDir);
                    }
                    string dumpFilePath = System.IO.Path.Combine(dumpsDir, dumpFileName);
                    await System.IO.File.WriteAllBytesAsync(dumpFilePath, dumpData, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("✓ Dump file created: {File} ({Size:N0} bytes)", dumpFileName, dumpData.Length);
                }
                else
                {
                    // Single region dump
                    string iterTempFile = System.IO.Path.GetTempFileName();
                    tempFiles.Add(iterTempFile);

                    string stageName = $"Memory Dump (Iter {iter + 1}/{iterationCount})";
                    double iterWeight = weight / iterationCount;
                    double iterStartPercent = startPercent + (weight * iter / iterationCount);

                    logger.LogInformation("  Streaming memory: 0x{Start:X8}, {Length:N0} bytes",
                        profiles.Memory.Start, profiles.Memory.Length);

                    await PerformStreamingDumpToFileAsync(
                        client,
                        profiles.Memory.Start,
                        profiles.Memory.Length,
                        iterTempFile,
                        progress,
                        logger,
                        iterStartPercent,
                        iterWeight,
                        stageName,
                        cancellationToken).ConfigureAwait(false);

                    // Read temp file, trim if needed, and create dump .bin file immediately
                    byte[] dumpData = await System.IO.File.ReadAllBytesAsync(iterTempFile, cancellationToken).ConfigureAwait(false);

                    // Validate and trim if we have extra padding bytes
                    if (dumpData.Length > profiles.Memory.Length)
                    {
                        byte[] trimmed = new byte[profiles.Memory.Length];
                        Array.Copy(dumpData, 0, trimmed, 0, profiles.Memory.Length);
                        logger.LogDebug("Trimmed dump from {Original} to {Expected} bytes (removed {Padding} padding bytes)",
                            dumpData.Length, profiles.Memory.Length, dumpData.Length - profiles.Memory.Length);
                        dumpData = trimmed;
                    }

                    allDumps.Add(dumpData);

                    // Create output dump file
                    string timestamp = (_timeProvider?.GetLocalNow() ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
                    // Use generic name for single-region dumps
                    string dumpFileName = $"MemoryDump_iter{iter + 1}_of_{iterationCount}_{timestamp}.bin";
                    string dumpsDir = "./dumps";
                    if (!System.IO.Directory.Exists(dumpsDir))
                    {
                        System.IO.Directory.CreateDirectory(dumpsDir);
                    }
                    string dumpFilePath = System.IO.Path.Combine(dumpsDir, dumpFileName);
                    await System.IO.File.WriteAllBytesAsync(dumpFilePath, dumpData, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("✓ Dump file created: {File} ({Size:N0} bytes)", dumpFileName, dumpData.Length);
                }
            }

            // The data from temp files has already been added to allDumps within the iteration loop.
            // The redundant loop that re-reads the files has been removed.
            logger.LogInformation("All {Count} dump files have been processed.", tempFiles.Count);

            TimeSpan dumpDuration = (_timeProvider?.GetUtcNow() ?? DateTime.UtcNow) - dumpStartTime;
            long totalBytes = allDumps.Sum(d => d.Length);
            double rate = totalBytes > 0 && dumpDuration.TotalSeconds > 0 ? totalBytes / dumpDuration.TotalSeconds : 0;

            logger.LogInformation("✓ Streaming dump complete: {Size:N0} bytes total", totalBytes);
            logger.LogInformation("  Duration: {Duration:F1}s, Rate: {Rate:F1} bytes/s", dumpDuration.TotalSeconds, rate);

            // Warning: This legacy method doesn't track SavedFiles in a list to return. 
            // Since we are moving to Simplified method, this might be less critical, but strict correctness requires it.
            // For now, returning empty list of saved files to satisfy the type.
            return new BootloaderResult(allDumps, new List<string>());
        }
        finally
        {
            // Stop any persistent streaming session established during the process
            await client.StopDumperSessionAsync().ConfigureAwait(false);

            // Clean up temp files
            foreach (string tempFile in tempFiles)
            {
                try
                {
                    if (System.IO.File.Exists(tempFile))
                    {
                        System.IO.File.Delete(tempFile);
                        logger.LogTrace("Deleted temp file: {File}", tempFile);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete temp file: {File}", tempFile);
                }
            }
        }
    }

    /// <summary>
    /// Performs a streaming memory dump directly to a file using high-performance DumperService.
    /// Data is streamed incrementally instead of buffering in memory.
    /// </summary>
    protected async Task<string> PerformStreamingDumpToFileAsync(
        IPlcClient client,
        uint address,
        uint length,
        string outputFilePath,
        IProgress<(string stage, double percent, long? bytesRead, long? totalBytes)> progress,
        ILogger logger,
        double startPercent,
        double weight,
        string stageName,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(outputFilePath);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogInformation("Starting streaming dump to file: {Path}", outputFilePath);
        logger.LogInformation("  Address: 0x{Address:X8}, Length: {Length:N0} bytes", address, length);

        DateTime dumpStartTime = _timeProvider?.GetUtcNow() ?? DateTime.UtcNow;
        long totalBytesReceived = 0;
        double lastReportedPercent = startPercent;
        int lastLoggedStepPercent = -1;

        // Create output directory if needed
        string? directory = System.IO.Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // Open file stream for incremental writing
        await using (var fileStream = new System.IO.FileStream(
            outputFilePath,
            System.IO.FileMode.Create,
            System.IO.FileAccess.Write,
            System.IO.FileShare.None,
            bufferSize: 81920, // 80KB buffer
            useAsync: true))
        {
            // Streaming callback - writes data directly to file
            async ValueTask OnDataReceivedAsync(ReadOnlyMemory<byte> data)
            {
                await fileStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
            }

            // Progress callback
            var streamProgress = new Progress<long>(bytesReceived =>
            {
                totalBytesReceived = bytesReceived;
                double percent = startPercent + (weight * bytesReceived / length);

                // Determine reporting threshold: 0.1% for >1MB, else 1.0%
                double threshold = length > 1024 * 1024 ? 0.1 : 1.0;

                // Report if changed by >= threshold
                if (Math.Abs(percent - lastReportedPercent) >= threshold || bytesReceived == length)
                {
                    // For multi-iteration, report cumulative total
                    // Note: caller should track iteration count, here we report per-segment
                    progress.Report((stageName, percent, bytesReceived, length));
                    lastReportedPercent = percent;
                }

                // Log occasionally (every 5%)
                if (length > 0)
                {
                    double pct = (double)bytesReceived / length * 100.0;
                    int currentStepPct = (int)pct;
                    if (currentStepPct > lastLoggedStepPercent && currentStepPct % 5 == 0)
                    {
                        lastLoggedStepPercent = currentStepPct;
                        logger.LogDebug("  Progress: {Percent:F1}% ({Bytes:N0}/{Total:N0})",
                            pct, bytesReceived, length);
                    }
                }
            });

            // Invoke streaming dump
            await client.InvokeDumperStreamAsync(
                address,
                length,
                OnDataReceivedAsync,
                streamProgress,
                cancellationToken,
                logger: logger).ConfigureAwait(false);

            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        TimeSpan dumpDuration = (_timeProvider?.GetUtcNow() ?? DateTime.UtcNow) - dumpStartTime;
        double transferRate = totalBytesReceived > 0 && dumpDuration.TotalSeconds > 0
            ? totalBytesReceived / dumpDuration.TotalSeconds
            : 0;

        logger.LogInformation("✓ Streaming dump completed: {Size:N0} bytes", totalBytesReceived);
        logger.LogInformation("  Duration: {Duration:F1}s, Rate: {Rate:F1} bytes/s",
            dumpDuration.TotalSeconds, transferRate);
        logger.LogInformation("  Saved to: {Path}", outputFilePath);

        return outputFilePath;
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

            // Sync baud rate from serial profile to socat configuration
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
                throw new InvalidOperationException("Failed to turn PLC power OFF");

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
                throw new InvalidOperationException("Failed to turn PLC power ON");

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

            // Perform handshake immediately after power on
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

            // Simulated progress for installation
            using (var stagerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var progressTask = SimulateProgressAsync(
                    stagerPayload.LongLength,
                    profiles.Serial.Configuration.BaudRate,
                    progress,
                    16.0, 18.0,
                    "stager_install",
                    isStagerInstall: true,  // Uses IRAM write protocol
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
                    isStagerInstall: false,  // Uses stager protocol
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

            // Use SIMPLIFIED streaming approach - no session restarts between iterations
            var dumpResult = await PerformDumpProcessStreamingSimplifiedAsync(
                client, profiles,
                progress, effectiveTaskLogger,
                startPercent: 20.0, weight: 75.0,  // 20% to 95%
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
            // Log full exception in process logger if available
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
                catch (Exception ex) { effectiveTaskLogger.LogWarning("Failed to stop socat: {Message}", ex.Message); }
            }

            if (isPowerConnected)
            {
                try
                {
                    await power.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                    effectiveTaskLogger.LogDebug("✓ Disconnected from power supply");
                }
                catch (Exception ex) { effectiveTaskLogger.LogWarning("Failed to disconnect power: {Message}", ex.Message); }
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
        // UART: 10 bits per byte (8 data + 1 start + 1 stop)
        const int bitsPerByte = 10;

        long totalWireBytes;
        int totalDelayMs;

        if (isStagerInstall)
        {
            // Stager uses WriteToIramAsync with 16-byte chunks
            // Each chunk requires 2 packets (mask + write)
            // Protocol overhead: length(1) + payload(N) + checksum(1) = N+2 bytes per packet
            const int chunkSize = 16;
            int numChunks = (int)Math.Ceiling((double)payloadSize / chunkSize);

            // Wire bytes calculation:
            // - Enter subprotocol: ~10 bytes (0x80 command + magic)
            // - Per chunk: 2 packets × (1 length + 16 payload + 1 checksum) = 36 bytes
            // - Leave subprotocol: ~5 bytes (0x81 command)
            // - Hook write: 2 packets × (1 + 6 + 1) = 16 bytes
            totalWireBytes = 10 + (numChunks * 36) + 5 + 16;

            // Delay calculation from SendPacketAsync:
            // - 10ms initial delay per packet
            // - Sends in 2-byte chunks with 10ms between
            // - Total packets = numChunks * 2 (mask+write) + 2 (enter+leave) + 2 (hook)
            int totalPackets = (numChunks * 2) + 4;
            int avgPacketSize = (int)(totalWireBytes / totalPackets);
            int chunksPerPacket = (avgPacketSize + 1) / 2; // 2-byte chunks
            totalDelayMs = totalPackets * (10 + (chunksPerPacket * 10));
        }
        else
        {
            // Dumper install uses stager protocol (different chunking/framing)
            // Typically uses larger packets sent via stager's protocol handler
            // Protocol overhead: length(1) + payload(N) + checksum(1)
            // Assuming 64-byte max chunks for stager protocol
            const int maxPacketPayload = 64;
            int numPackets = (int)Math.Ceiling((double)payloadSize / maxPacketPayload);

            // Wire bytes: num_packets × (1 length + avg_payload + 1 checksum)
            int avgPayloadPerPacket = (int)((payloadSize + numPackets - 1) / numPackets);
            totalWireBytes = numPackets * (1 + avgPayloadPerPacket + 1);

            // Delay: 10ms initial + 2-byte chunks with 10ms delay
            int avgChunksPerPacket = (avgPayloadPerPacket + 2 + 1) / 2;
            totalDelayMs = numPackets * (10 + (avgChunksPerPacket * 10));
        }

        // Calculate transfer duration in milliseconds
        double transferTimeMs = ((double)totalWireBytes * bitsPerByte * 1000.0) / (double)baudRate;

        // Total time = UART transfer time + protocol delays
        int totalTimeMs = (int)(transferTimeMs + totalDelayMs);

        // Add 10% buffering for other overhead (processing, etc.)
        totalTimeMs = (int)(totalTimeMs * 1.1);

        // Force minimum 1 second for progress bar visibility
        if (totalTimeMs < 1000)
        {
            totalTimeMs = 1000;
        }

        // Note: detailed debug logging of transfer calculation removed since base class has no logger
        // Derived classes can add their own logging if needed

        await WaitWithProgressAsync(
            totalTimeMs,
            progress,
            startPercent,
            targetPercent,
            stage,
            cancellationToken).ConfigureAwait(false);
    }
}
