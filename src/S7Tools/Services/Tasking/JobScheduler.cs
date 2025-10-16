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
    private readonly ConcurrentQueue<Job> _queue = new();
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();
    private int _processingCount;

    /// <inheritdoc />
    public event JobStateChanged? JobStateChanged;

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
    public Guid Enqueue(Job job)
    {
        ArgumentNullException.ThrowIfNull(job);

        _logger.LogInformation("Enqueuing job {JobId} ({JobName}) with {ResourceCount} resources",
            job.Id, job.Name, job.Resources.Count);

        Job queuedJob = job with { State = JobState.Queued };
        _jobs[job.Id] = queuedJob;
        _queue.Enqueue(queuedJob);

        JobStateChanged?.Invoke(job.Id, JobState.Queued, null);

        // Try to start processing immediately
        _ = Task.Run(() => TryStartNextAsync());

        return job.Id;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Job> GetAll()
    {
        return _jobs.Values.ToList();
    }

    private Task TryStartNextAsync()
    {
        // Prevent concurrent processing attempts
        if (Interlocked.CompareExchange(ref _processingCount, 1, 0) != 0)
        {
            return Task.CompletedTask;
        }

        try
        {
            while (_queue.TryDequeue(out Job? job))
            {
                // Try to acquire resources for this job
                if (!_resources.TryAcquire(job.Resources))
                {
                    // Resources not available, re-queue for later
                    _queue.Enqueue(job);
                    _logger.LogDebug("Job {JobId} waiting for resources", job.Id);
                    break;
                }

                // Resources acquired, start execution
                _logger.LogInformation("Starting job {JobId} ({JobName})", job.Id, job.Name);
                _ = ExecuteJobAsync(job);
            }
        }
        finally
        {
            Interlocked.Exchange(ref _processingCount, 0);
        }

        return Task.CompletedTask;
    }

    private async Task ExecuteJobAsync(Job job)
    {
        try
        {
            // Update job state to running
            Job runningJob = job with { State = JobState.Running };
            _jobs[job.Id] = runningJob;
            JobStateChanged?.Invoke(job.Id, JobState.Running, "Starting bootloader operation");

            // Create progress reporter
            var progress = new Progress<(string stage, double percent)>(p =>
            {
                var message = $"{p.stage}: {p.percent:P0}";
                JobStateChanged?.Invoke(job.Id, JobState.Running, message);
                _logger.LogDebug("Job {JobId} progress: {Stage} - {Percent:P0}",
                    job.Id, p.stage, p.percent);
            });

            // Execute the dump operation
            var dumpData = await _bootloader.DumpAsync(job.Profiles, progress, CancellationToken.None)
                .ConfigureAwait(false);

            // Save dump to file
            var outputFile = Path.Combine(job.Profiles.OutputPath, $"dump-{job.Id}.bin");
            Directory.CreateDirectory(job.Profiles.OutputPath);
            await File.WriteAllBytesAsync(outputFile, dumpData, CancellationToken.None)
                .ConfigureAwait(false);

            // Update job state to completed
            Job completedJob = job with { State = JobState.Completed };
            _jobs[job.Id] = completedJob;
            JobStateChanged?.Invoke(job.Id, JobState.Completed, outputFile);

            _logger.LogInformation("Job {JobId} completed successfully. Output: {OutputFile}",
                job.Id, outputFile);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Job {JobId} was canceled", job.Id);
            Job canceledJob = job with { State = JobState.Canceled };
            _jobs[job.Id] = canceledJob;
            JobStateChanged?.Invoke(job.Id, JobState.Canceled, "Operation canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed with error: {ErrorMessage}",
                job.Id, ex.Message);

            Job failedJob = job with { State = JobState.Failed };
            _jobs[job.Id] = failedJob;
            JobStateChanged?.Invoke(job.Id, JobState.Failed, ex.Message);
        }
        finally
        {
            // Release resources
            _resources.Release(job.Resources);
            _logger.LogDebug("Released resources for job {JobId}", job.Id);

            // Try to start the next job
            _ = Task.Run(() => TryStartNextAsync());
        }
    }
}
