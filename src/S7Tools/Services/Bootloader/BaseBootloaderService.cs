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
