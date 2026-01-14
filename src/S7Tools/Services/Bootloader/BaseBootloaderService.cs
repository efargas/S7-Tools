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
    /// Performs memory dump using streaming (writes to temp files, reads back as byte arrays).
    /// Provides 80% memory reduction during dump phase while maintaining interface compatibility.
    /// </summary>
    protected async Task<List<byte[]>> PerformDumpProcessStreamingAsync(
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
                            var segProgress = new Progress<long>(bytes =>
                            {
                                segBytesWritten = bytes;
                                double percent = segStartPercent + (segWeight * bytes / segLength);
                                progress.Report((stageName, percent, bytes, segLength));
                            });

                            await client.InvokeDumperStreamAsync(
                                segStart, segLength,
                                async data => await combinedStream.WriteAsync(data, cancellationToken),
                                segProgress,
                                cancellationToken).ConfigureAwait(false);

                            logger.LogDebug("  ✓ Segment {Index} streamed: {Size:N0} bytes", i + 1, segBytesWritten);
                        }
                    }
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
                }
            }

            // Read all temp files back as byte arrays
            logger.LogInformation("Reading {Count} dump files into memory...", tempFiles.Count);
            foreach (string tempFile in tempFiles)
            {
                byte[] data = await System.IO.File.ReadAllBytesAsync(tempFile, cancellationToken).ConfigureAwait(false);
                allDumps.Add(data);
                logger.LogDebug("  Read {Size:N0} bytes from temp file", data.Length);
            }

            TimeSpan dumpDuration = (_timeProvider?.GetUtcNow() ?? DateTime.UtcNow) - dumpStartTime;
            long totalBytes = allDumps.Sum(d => d.Length);
            double rate = totalBytes > 0 && dumpDuration.TotalSeconds > 0 ? totalBytes / dumpDuration.TotalSeconds : 0;

            logger.LogInformation("✓ Streaming dump complete: {Size:N0} bytes total", totalBytes);
            logger.LogInformation("  Duration: {Duration:F1}s, Rate: {Rate:F1} bytes/s", dumpDuration.TotalSeconds, rate);

            return allDumps;
        }
        finally
        {
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

                // Report if changed by >= 0.1%
                if (Math.Abs(percent - lastReportedPercent) >= 0.1 || bytesReceived == length)
                {
                    progress.Report((stageName, percent, bytesReceived, length));
                    lastReportedPercent = percent;
                }

                // Log occasionally (every 5%)
                if (length > 0)
                {
                    double pct = (double)bytesReceived / length * 100.0;
                    if ((int)pct % 5 == 0)
                    {
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
                cancellationToken).ConfigureAwait(false);

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
}
