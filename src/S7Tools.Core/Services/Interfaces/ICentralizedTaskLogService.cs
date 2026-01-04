using System;

namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Service that manages centralized logging stores for tasks.
/// allows for retrieving or creating specific log data stores based on task IDs.
/// </summary>
public interface ICentralizedTaskLogService
{
    /// <summary>
    /// Retrieves or creates a set of log data stores (Main, Process, Protocol) for a specific task.
    /// </summary>
    /// <param name="taskId">The unique identifier of the task.</param>
    /// <returns>A tuple containing the Main, Process, and Protocol log data stores.</returns>
    (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) GetOrCreateStoresForTask(Guid taskId);
}
