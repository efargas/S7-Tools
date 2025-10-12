// IJobScheduler.cs
namespace S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Models.Jobs;
public interface IJobScheduler {
    event JobStateChanged? JobStateChanged;
    Guid Enqueue(Job job);
    IReadOnlyCollection<Job> GetAll();
}
public delegate void JobStateChanged(Guid jobId, JobState state, string? message);