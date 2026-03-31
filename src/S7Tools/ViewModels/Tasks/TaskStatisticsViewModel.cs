using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.ViewModels.Base;

namespace S7Tools.ViewModels.Tasks;

/// <summary>
/// ViewModel responsible for calculating and exposing task execution statistics.
/// </summary>
public class TaskStatisticsViewModel : ViewModelBase
{
    private int _totalTasksCount;
    private int _runningTasksCount;
    private int _failedTasksCount;
    private TimeSpan _averageExecutionTime;
    private string _resourceUtilization = string.Empty;

    /// <summary>
    /// Gets the total number of tasks across all states.
    /// </summary>
    public int TotalTasksCount
    {
        get => _totalTasksCount;
        private set => this.RaiseAndSetIfChanged(ref _totalTasksCount, value);
    }

    /// <summary>
    /// Gets the number of currently running tasks.
    /// </summary>
    public int RunningTasksCount
    {
        get => _runningTasksCount;
        private set => this.RaiseAndSetIfChanged(ref _runningTasksCount, value);
    }

    /// <summary>
    /// Gets the number of failed tasks.
    /// </summary>
    public int FailedTasksCount
    {
        get => _failedTasksCount;
        private set => this.RaiseAndSetIfChanged(ref _failedTasksCount, value);
    }

    /// <summary>
    /// Gets the average execution time for completed tasks.
    /// </summary>
    public TimeSpan AverageExecutionTime
    {
        get => _averageExecutionTime;
        private set => this.RaiseAndSetIfChanged(ref _averageExecutionTime, value);
    }

    /// <summary>
    /// Gets the current resource utilization summary.
    /// </summary>
    public string ResourceUtilization
    {
        get => _resourceUtilization;
        private set => this.RaiseAndSetIfChanged(ref _resourceUtilization, value);
    }

    /// <summary>
    /// Updates the statistics based on the current state of task collections.
    /// </summary>
    public void Update(
        int createdCount,
        int queuedCount,
        int scheduledCount,
        IEnumerable<TaskExecution> activeTasks,
        IEnumerable<TaskExecution> finishedTasks)
    {
        // Materialize collections to avoid multiple enumerations if they are LINQ queries (though typically passed as ObservableCollection)
        var activeList = activeTasks as ICollection<TaskExecution> ?? activeTasks.ToList();
        var finishedList = finishedTasks as ICollection<TaskExecution> ?? finishedTasks.ToList();

        int allTasksCount = createdCount + queuedCount + scheduledCount + activeList.Count + finishedList.Count;
        TotalTasksCount = allTasksCount;

        RunningTasksCount = activeList.Count(t => t.State == TaskState.Running);
        FailedTasksCount = finishedList.Count(t => t.State == TaskState.Failed);

        // Calculate average execution time for completed tasks
        var completedTasks = finishedList.Where(t => t.State == TaskState.Completed && t.ExecutionTime.HasValue).ToList();
        if (completedTasks.Count > 0)
        {
            double totalTime = completedTasks.Sum(t => t.ExecutionTime!.Value.TotalMilliseconds);
            AverageExecutionTime = TimeSpan.FromMilliseconds(totalTime / completedTasks.Count);
        }
        else
        {
            AverageExecutionTime = TimeSpan.Zero;
        }

        // Update resource utilization summary
        int activeResourceCount = activeList.SelectMany(t => t.LockedResources).Distinct().Count();
        ResourceUtilization = $"{activeResourceCount} resources in use";
    }
}
