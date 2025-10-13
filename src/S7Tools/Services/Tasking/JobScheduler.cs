// JobScheduler.cs
namespace S7Tools.Services.Tasking;

using Microsoft.Extensions.Logging;
using S7Tools.Core.Models.Jobs;
using S7Tools.Core.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public sealed class JobScheduler : IJobScheduler {
    private readonly ILogger<JobScheduler> _logger;
    private readonly IResourceCoordinator _resources;
    private readonly IBootloaderService _bootloader;
    private readonly ConcurrentQueue<Job> _queue = new();
    private readonly ConcurrentDictionary<Guid, Job> _jobs = new();

    public event JobStateChanged? JobStateChanged;

    public JobScheduler(ILogger<JobScheduler> logger, IResourceCoordinator resources, IBootloaderService bootloader) {
        _logger = logger; _resources = resources; _bootloader = bootloader;
    }

    public Guid Enqueue(Job job) {
        _jobs[job.Id] = job with { State = JobState.Queued };
        _queue.Enqueue(job);
        JobStateChanged?.Invoke(job.Id, JobState.Queued, null);
        _ = TryStartNextAsync();
        return job.Id;
    }

    public IReadOnlyCollection<Job> GetAll() => _jobs.Values.ToList();

    private async Task TryStartNextAsync() {
        if (!_queue.TryDequeue(out var job)) return;
        if (!_resources.TryAcquire(job.Resources)) {
            _queue.Enqueue(job); // Re-queue the job
            return;
        }
        _jobs[job.Id] = job with { State = JobState.Running };
        JobStateChanged?.Invoke(job.Id, JobState.Running, null);
        try {
            var progress = new Progress<(string stage, double pct)>(p => JobStateChanged?.Invoke(job.Id, JobState.Running, $"{p.stage}:{p.pct:P0}"));
            var bytes = await _bootloader.DumpAsync(job.Profiles, progress, CancellationToken.None).ConfigureAwait(false);
            var outFile = Path.Combine(job.Profiles.OutputPath, $"dump-{job.Id}.bin");
            Directory.CreateDirectory(job.Profiles.OutputPath);
            await File.WriteAllBytesAsync(outFile, bytes).ConfigureAwait(false);
            _jobs[job.Id] = job with { State = JobState.Completed };
            JobStateChanged?.Invoke(job.Id, JobState.Completed, outFile);
        } catch (Exception ex) {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);
            _jobs[job.Id] = job with { State = JobState.Failed };
            JobStateChanged?.Invoke(job.Id, JobState.Failed, ex.Message);
        } finally {
            _resources.Release(job.Resources);
            _ = TryStartNextAsync();
        }
    }
}