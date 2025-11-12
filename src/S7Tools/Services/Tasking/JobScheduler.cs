using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Tasking;

/// <summary>
/// Manages job scheduling, execution, and resource coordination.
/// Executes jobs in parallel when resources allow, queuing conflicting jobs.
/// </summary>
public sealed class JobScheduler : IJobScheduler
{
    private readonly ILogger<JobScheduler> _logger;
    private readonly IResourceCoordinator _resources;
    private readonly IBootloaderService _bootloader;
    private readonly ConcurrentDictionary<int, Job> _jobs = new();
    private readonly SemaphoreSlim _schedulerLock = new(1, 1);
    private CancellationTokenSource? _schedulerCts;
    private Task? _schedulerTask;

    /// <inheritdoc />
    public event EventHandler<JobStateChangedEventArgs>? JobStateChanged;

    /// <inheritdoc />
    public event EventHandler<JobProgressChangedEventArgs>? JobProgressChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobScheduler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostics.</param>
    /// <param name="resources">Resource coordinator for managing exclusive resource access.</param>
    /// <param name="bootloader">Bootloader service for job execution.</param>
    public JobScheduler(
        ILogger<JobScheduler> logger,
        IResourceCoordinator resources,
        IBootloaderService bootloader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _bootloader = bootloader ?? throw new ArgumentNullException(nameof(bootloader));
    }

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
            QueuedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
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
            CompletedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
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
        await _schedulerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_schedulerTask != null)
            {
                throw new InvalidOperationException("Scheduler already running");
            }

            _schedulerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _schedulerTask = Task.Run(() => ProcessQueueAsync(_schedulerCts.Token), _schedulerCts.Token);

            _logger.LogInformation("Job scheduler started");
        }
        finally
        {
            _schedulerLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(TimeSpan gracefulTimeout, CancellationToken cancellationToken = default)
    {
        await _schedulerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
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
        }
        finally
        {
            _schedulerLock.Release();
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Scheduler queue processing started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Find queued jobs
                Job[] queuedJobs = _jobs.Values
                    .Where(j => j.State == JobState.Queued)
                    .ToArray();

                foreach (Job job in queuedJobs)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Try to acquire resources (stub - needs actual resource extraction)
                    var resources = new[] { new ResourceKey("serial", "/dev/ttyUSB0") };

                    if (_resources.TryAcquire(resources))
                    {
                        // Start execution in background
                        _ = ExecuteJobAsync(job, resources, cancellationToken);
                    }
                }

                // Wait before next cycle
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

        _logger.LogDebug("Scheduler queue processing stopped");
    }

    private async Task ExecuteJobAsync(Job job, ResourceKey[] resources, CancellationToken cancellationToken)
    {
        try
        {
            // Update to Running
            Job runningJob = job with
            {
                State = JobState.Running,
                StartedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            _jobs[job.Id] = runningJob;

            JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                job.Id,
                JobState.Queued,
                JobState.Running,
                "Starting bootloader operation"));

            // Execute (stub implementation for now)
            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

            // Complete
            Job completedJob = runningJob with
            {
                State = JobState.Completed,
                Progress = 100.0,
                CompletedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            _jobs[job.Id] = completedJob;

            JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                job.Id,
                JobState.Running,
                JobState.Completed,
                null));

            _logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);

            Job failedJob = job with
            {
                State = JobState.Failed,
                ErrorMessage = ex.Message,
                CompletedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };
            _jobs[job.Id] = failedJob;

            JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(
                job.Id,
                JobState.Running,
                JobState.Failed,
                ex.Message));
        }
        finally
        {
            _resources.Release(resources);
        }
    }
}
