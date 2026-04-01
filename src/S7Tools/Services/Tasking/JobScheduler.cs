using System.Collections.Concurrent;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;
using S7Tools.Core.Models.Jobs;
using S7Tools.Extensions;

namespace S7Tools.Services.Tasking;

/// <summary>
/// Manages job scheduling, execution, and resource coordination.
/// Executes jobs in parallel when resources allow, queuing conflicting jobs.
/// </summary>
public sealed class JobScheduler(
    ILogger<JobScheduler> logger,
    IResourceCoordinator resources,
    IBootloaderService bootloader,
    ITimeProvider timeProvider)
    : IJobScheduler, IDisposable
{
    private readonly ILogger<JobScheduler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IResourceCoordinator _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    private readonly IBootloaderService _bootloader = bootloader ?? throw new ArgumentNullException(nameof(bootloader));
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ConcurrentDictionary<int, Job> _jobs = [];
    private readonly ConcurrentDictionary<int, Task> _runningJobs = [];
    private readonly SemaphoreSlim _schedulerLock = new(1, 1);
    private CancellationTokenSource? _schedulerCts;
    private Task? _schedulerTask;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler<JobStateChangedEventArgs>? JobStateChanged;

    /// <inheritdoc />
    public event EventHandler<JobProgressChangedEventArgs>? JobProgressChanged;

    /// <inheritdoc />
    public Task<Job> EnqueueAsync(Job job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        if (job.State != JobState.Created)
        {
            throw new InvalidOperationException($"Job must be in Created state to enqueue (current: {job.State})");
        }

        _logger.LogInformation("Enqueuing job {JobId} ({JobName})",
            job.Id, job.Name);

        Job queuedJob = job with
        {
            State = JobState.Queued,
            QueuedAt = _timeProvider.GetLocalNow(),
            ModifiedAt = _timeProvider.GetLocalNow()
        };

        _jobs[job.Id] = queuedJob;

        JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
            job.Id,
            JobState.Created,
            JobState.Queued,
            null));

        return Task.FromResult(queuedJob);
    }

    /// <inheritdoc />
    public Task<bool> CancelJobAsync(int jobId, CancellationToken cancellationToken = default)
    {
        if (!_jobs.TryGetValue(jobId, out Job? job))
        {
            return Task.FromResult(false);
        }

        // Can only cancel if Queued or Running
        if (job.State != JobState.Queued && job.State != JobState.Running)
        {
            return Task.FromResult(false);
        }

        Job canceledJob = job with
        {
            State = JobState.Canceled,
            CompletedAt = _timeProvider.GetLocalNow(),
            ModifiedAt = _timeProvider.GetLocalNow()
        };

        _jobs[jobId] = canceledJob;

        JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
            jobId,
            job.State,
            JobState.Canceled,
            "Job canceled by user"));

        _logger.LogInformation("Job {JobId} canceled", jobId);

        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Job>> GetJobsByStateAsync(
        JobState[]? states = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Job> query = _jobs.Values;

        if (states != null && states.Length > 0)
        {
            HashSet<JobState> stateSet = new(states);
            query = query.Where(j => stateSet.Contains(j.State));
        }

        IReadOnlyList<Job> result = query.ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<Job?> GetJobByIdAsync(int jobId, CancellationToken cancellationToken = default)
    {
        _jobs.TryGetValue(jobId, out Job? job);
        return Task.FromResult(job);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _schedulerLock.ExecuteAsync(async () =>
        {
            if (_schedulerTask != null)
            {
                throw new InvalidOperationException("Scheduler already running");
            }

            _schedulerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _schedulerTask = Task.Run(() => ProcessQueueAsync(_schedulerCts.Token), _schedulerCts.Token);

            _logger.LogInformation("Job scheduler started");
            await Task.CompletedTask;
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken = default)
    {
        await _schedulerLock.ExecuteAsync(async () =>
        {
            if (_schedulerTask == null || _schedulerCts == null)
            {
                return; // Not running
            }

            _schedulerCts.Cancel();

            Task completedTask = await Task.WhenAny(_schedulerTask, Task.Delay(gracefulTimeout, cancellationToken))
                .ConfigureAwait(false);

            if (completedTask != _schedulerTask)
            {
                _logger.LogWarning("Scheduler did not stop within graceful timeout");
            }

            _schedulerTask = null;
            _schedulerCts?.Dispose();
            _schedulerCts = null;

            _logger.LogInformation("Job scheduler stopped");
        }, cancellationToken);
    }

    /// <summary>
    /// Extracts resource keys from a job's profile set for resource coordination.
    /// </summary>
    /// <param name="profileSet">The job profile set containing resource configurations.</param>
    /// <returns>Array of resource keys for serial port, TCP port, and modbus connection.</returns>
    private static ResourceKey[] ExtractResources(JobProfileSet profileSet)
    {
        ArgumentNullException.ThrowIfNull(profileSet);

        return
        [
            new ResourceKey("serial", profileSet.Serial.Device),
            new ResourceKey("tcp", profileSet.Socat.Port.ToString()),
            new ResourceKey("modbus", $"{profileSet.Power.Host}:{profileSet.Power.Port}")
        ];
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scheduler queue processing started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // STEP 1: Clean up completed jobs
                foreach (KeyValuePair<int, Task> kvp in _runningJobs.ToArray())
                {
                    if (kvp.Value.IsCompleted)
                    {
                        _runningJobs.TryRemove(kvp.Key, out _);
                        _logger.LogDebug("Removed completed job {JobId} from running jobs", kvp.Key);
                    }
                }

                // STEP 2: Find queued jobs that can start
                Job[] queuedJobs = _jobs.Values
                    .Where(j => j.State == JobState.Queued)
                    .ToArray();

                // Only log when there are queued jobs (avoid log spam when idle)
                if (queuedJobs.Length > 0)
                {
                    _logger.LogDebug("Found {Count} queued jobs, {RunningCount} currently running",
                        queuedJobs.Length, _runningJobs.Count);
                }

                // STEP 3: Collect ALL jobs that can acquire resources (snapshot approach)
                var jobsToStart = new List<(Job job, ResourceKey[] resources)>();

                foreach (Job job in queuedJobs)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (_runningJobs.ContainsKey(job.Id))
                    {
                        continue;
                    }

                    ResourceKey[] resources = ExtractResources(job.ProfileSet);

                    // Try to acquire - if successful, add to batch
                    if (_resources.TryAcquire(resources))
                    {
                        jobsToStart.Add((job, resources));
                        _logger.LogDebug("Reserved resources for job {JobId}", job.Id);
                    }
                    else
                    {
                        _logger.LogDebug("Job {JobId} waiting for resources: {Resources}",
                            job.Id, string.Join(", ", resources.Select(r => $"{r.Kind}:{r.Id}")));
                    }
                }

                // Only log when actually starting jobs (avoid log spam when idle)
                if (jobsToStart.Count > 0)
                {
                    _logger.LogInformation("Starting batch of {Count} jobs simultaneously", jobsToStart.Count);
                }

                // STEP 4: Update ALL states synchronously FIRST
                foreach ((Job? job, ResourceKey[]? resources) in jobsToStart)
                {
                    Job runningJob = job with
                    {
                        State = JobState.Running,
                        StartedAt = _timeProvider.GetLocalNow(),
                        ModifiedAt = _timeProvider.GetLocalNow()
                    };
                    _jobs[job.Id] = runningJob;

                    // Fire event synchronously (CRITICAL for maxConcurrent tracking)
                    JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                        job.Id,
                        JobState.Queued,
                        JobState.Running,
                        $"Starting bootloader operation (batch of {jobsToStart.Count})"));
                }

                // STEP 5: Launch ALL tasks AFTER state changes complete
                foreach ((Job? job, ResourceKey[]? resources) in jobsToStart)
                {
                    Job runningJob = _jobs[job.Id];  // Get updated job with Running state
                    Task executionTask = Task.Run(
                        () => ExecuteJobAsync(runningJob, resources, cancellationToken),
                        cancellationToken);
                    _runningJobs.TryAdd(job.Id, executionTask);
                }

                // STEP 6: Wait before next cycle (500ms polling interval)
                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scheduler queue processing");
            }
        }

        // Wait for all running jobs to complete on shutdown
        if (_runningJobs.Any())
        {
            _logger.LogInformation("Waiting for {Count} running jobs to complete", _runningJobs.Count);
            await Task.WhenAll(_runningJobs.Values).ConfigureAwait(false);
        }

        _logger.LogDebug("Scheduler queue processing stopped");
    }

    private async Task ExecuteJobAsync(Job job, ResourceKey[] resources, CancellationToken cancellationToken)
    {
        try
        {
            // Job is already in Running state (transitioned in ProcessQueueAsync)
            _logger.LogInformation("Job {JobId} executing bootloader operation", job.Id);

            // STEP 1: Execute actual bootloader operation
            var progress = new Progress<(string stage, double percent, long? bytesRead, long? totalBytes)>(p =>
            {
                // Update job progress
                double percentage = p.percent;
                string operation = GetUserFriendlyOperationName(p.stage);

                Job progressJob = _jobs[job.Id] with
                {
                    Progress = percentage,
                    CurrentOperation = operation,
                    ModifiedAt = _timeProvider.GetLocalNow()
                };
                _jobs[job.Id] = progressJob;

                // Raise progress event
                JobProgressChanged?.Invoke(this, new JobProgressChangedEventArgs(
                    job.Id,
                    percentage,
                    operation));

                _logger.LogDebug("Job {JobId} progress: {Stage} ({Percent:F1}%)",
                    job.Id, operation, percentage);
            });

            // Execute bootloader dump
            // Execute bootloader dump
            // Execute bootloader dump
            BootloaderResult result = await _bootloader.DumpAsync(
                job.ProfileSet,
                progress,
                null, // No task logger in JobScheduler (legacy path)
                null, // No process logger in JobScheduler (legacy path)
                cancellationToken).ConfigureAwait(false);

            // STEP 2: Log dump data saving (files already saved by service)
            string outputPath = job.ProfileSet.OutputPath;
            // Directory creation handled by service

            long totalSize = result.SavedFiles.Sum(x => (long)x.Length);

            if (result.SavedFiles.Count > 0)
            {
                string primaryFile = result.SavedFiles[0];
                if (result.SavedFiles.Count == 1)
                {
                    _logger.LogInformation("Job {JobId} dump saved to {Path} ({Size} bytes)",
                       job.Id, primaryFile, totalSize);
                }
                else
                {
                    _logger.LogInformation("Job {JobId} saved {Count} dump files to {Path} (Total {Size} bytes)",
                       job.Id, result.SavedFiles.Count, Path.GetDirectoryName(primaryFile), totalSize);
                }
            }

            // STEP 3: Transition to Completed state
            Job completedJob = job with
            {
                State = JobState.Completed,
                Progress = 100.0,
                CurrentOperation = "Complete",
                CompletedAt = _timeProvider.GetLocalNow(),
                ModifiedAt = _timeProvider.GetLocalNow()
            };
            _jobs[job.Id] = completedJob;

            JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                job.Id,
                JobState.Running,
                JobState.Completed,
                null));

            _logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (PartialDumpException pde)
        {
            // Dump was interrupted but partial files were preserved
            bool wasCancelled = pde.InnerException is OperationCanceledException;
            string partialState = wasCancelled ? "canceled" : "failed";
            foreach (string partialFile in pde.PartialResult.SavedFiles)
            {
                _logger.LogWarning("Job {JobId} dump file preserved: {FilePath}", job.Id, partialFile);
            }

            string partialMsg = $"Dump {partialState} with {pde.PartialResult.SavedFiles.Count} dump file(s) preserved (some may be partial)";
            JobState resultState = wasCancelled ? JobState.Canceled : JobState.Failed;

            Job partialJob = job with
            {
                State = resultState,
                ErrorMessage = partialMsg,
                CompletedAt = _timeProvider.GetLocalNow(),
                ModifiedAt = _timeProvider.GetLocalNow()
            };
            _jobs[job.Id] = partialJob;

            JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                job.Id,
                JobState.Running,
                resultState,
                partialJob.ErrorMessage));

            _logger.LogWarning("Job {JobId} dump {State} with {Count} dump file(s) preserved (some may be partial)",
                job.Id, partialState, pde.PartialResult.SavedFiles.Count);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is expected - transition to Canceled state
            Job canceledJob = job with
            {
                State = JobState.Canceled,
                ErrorMessage = "Operation canceled by user",
                CompletedAt = _timeProvider.GetLocalNow(),
                ModifiedAt = _timeProvider.GetLocalNow()
            };
            _jobs[job.Id] = canceledJob;

            JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                job.Id,
                JobState.Running,
                JobState.Canceled,
                "Operation canceled"));

            _logger.LogWarning("Job {JobId} canceled", job.Id);
            throw; // Re-throw to propagate cancellation
        }
        catch (Exception ex)
        {
            // Unexpected error - transition to Failed state
            _logger.LogError(ex, "Job {JobId} failed: {ErrorMessage}", job.Id, ex.Message);

            Job failedJob = job with
            {
                State = JobState.Failed,
                ErrorMessage = ex.Message,
                CompletedAt = _timeProvider.GetLocalNow(),
                ModifiedAt = _timeProvider.GetLocalNow()
            };
            _jobs[job.Id] = failedJob;

            JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                job.Id,
                JobState.Running,
                JobState.Failed,
                ex.Message));

            // DO NOT re-throw - error handled, resources will be released in finally
        }
        finally
        {
            // CRITICAL: Always release resources, even on exception or cancellation
            _resources.Release(resources);

            _logger.LogInformation("Resources released for job {JobId}: {Resources}",
                job.Id, string.Join(", ", resources.Select(r => $"{r.Kind}:{r.Id}")));
        }
    }

    /// <summary>
    /// Converts technical stage names to user-friendly operation descriptions.
    /// </summary>
    private static string GetUserFriendlyOperationName(string stage)
    {
        return stage switch
        {
            "socat_setup" => "Setting up network bridge",
            "power_cycle" => "Power cycling PLC",
            "handshake" => "Establishing bootloader connection",
            "stager_install" => "Installing stager payload",
            "memory_dump" => "Dumping memory",
            "teardown" => "Cleaning up resources",
            "complete" => "Operation complete",
            _ => stage.Replace("_", " ")
        };
    }

    /// <summary>
    /// Disposes the JobScheduler and releases managed resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _schedulerLock.Dispose();
        _schedulerCts?.Dispose();
        _disposed = true;
    }
}
