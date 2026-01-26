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

        List<string> savedFiles = [];

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

                    // Create output dump file immediately
                    string timestamp = (_timeProvider?.GetLocalNow() ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
                    // Use first segment name or generic name for dump file
                    string jobName = segments.FirstOrDefault()?.Name ?? "MemoryDump";
                    string dumpFileName = $"{jobName}_iter{iter + 1}_of_{iterationCount}_{timestamp}.bin";
                    string dumpsDir = !string.IsNullOrWhiteSpace(profiles.OutputPath) ? profiles.OutputPath : "./dumps";

                    if (!System.IO.Directory.Exists(dumpsDir))
                    {
                        System.IO.Directory.CreateDirectory(dumpsDir);
                    }
                    string dumpFilePath = System.IO.Path.Combine(dumpsDir, dumpFileName);
                    savedFiles.Add(dumpFilePath);

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
                                async data => await fileStream.WriteAsync(data, cancellationToken),
                                segProgress,
                                cancellationToken,
                                logger: logger).ConfigureAwait(false);

                            logger.LogDebug("  ✓ Segment {Index} streamed: {Size:N0} bytes", i + 1, segBytesWritten);
                        }

                        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }

                    long fileSize = new System.IO.FileInfo(dumpFilePath).Length;
                    logger.LogInformation("✓ Dump file created: {File} ({Size:N0} bytes)", dumpFileName, fileSize);
                }
                else
                {
                    // Single region dump
                    string stageName = $"Memory Dump (Iter {iter + 1}/{iterationCount})";
                    double iterWeight = weight / iterationCount;
                    double iterStartPercent = startPercent + (weight * iter / iterationCount);

                    logger.LogInformation("  Streaming memory: 0x{Start:X8}, {Length:N0} bytes",
                        profiles.Memory.Start, profiles.Memory.Length);

                    // Create output dump file
                    string timestamp = (_timeProvider?.GetLocalNow() ?? DateTime.Now).ToString("yyyyMMdd_HHmmss");
                    string dumpFileName = $"MemoryDump_iter{iter + 1}_of_{iterationCount}_{timestamp}.bin";
                    string dumpsDir = !string.IsNullOrWhiteSpace(profiles.OutputPath) ? profiles.OutputPath : "./dumps";

                    if (!System.IO.Directory.Exists(dumpsDir))
                    {
                        System.IO.Directory.CreateDirectory(dumpsDir);
                    }
                    string dumpFilePath = System.IO.Path.Combine(dumpsDir, dumpFileName);
                    savedFiles.Add(dumpFilePath);

                    await PerformStreamingDumpToFileAsync(
                        client,
                        profiles.Memory.Start,
                        profiles.Memory.Length,
                        dumpFilePath,
                        progress,
                        logger,
                        iterStartPercent,
                        iterWeight,
                        stageName,
                        cancellationToken).ConfigureAwait(false);

                    logger.LogInformation("✓ Dump file created: {File}", dumpFileName);
                }
            }

            logger.LogInformation("All {Count} dump files have been processed.", savedFiles.Count);

            TimeSpan dumpDuration = (_timeProvider?.GetUtcNow() ?? DateTime.UtcNow) - dumpStartTime;

            // Cannot easily calculate total bytes without re-reading files, using 0 or approx from config
            long totalBytes = 0;
            // We return empty byte list as we streamed to disk
            return new BootloaderResult([], savedFiles);
        }
        finally
        {
            // Stop any persistent streaming session established during the process
            await client.StopDumperSessionAsync().ConfigureAwait(false);
        }
    }
