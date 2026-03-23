using System;
using System.Collections.Concurrent;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// Centralizes the management of task log data stores, providing per-task log store access.
/// </summary>
/// <remarks>
/// Uses a <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}"/> to provide thread-safe
/// creation and retrieval of log data stores for main, process, and protocol log categories.
/// </remarks>
/// <param name="taskLogDataStoreFactory">The factory used to create task log data stores.</param>
public class CentralizedTaskLogService(ITaskLogDataStoreFactory taskLogDataStoreFactory) : ICentralizedTaskLogService
{
    private readonly ConcurrentDictionary<Guid, (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog)> _taskLogs = new();
    private readonly ITaskLogDataStoreFactory _taskLogDataStoreFactory = taskLogDataStoreFactory ?? throw new ArgumentNullException(nameof(taskLogDataStoreFactory));

    /// <inheritdoc />
    public (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) GetOrCreateStoresForTask(Guid taskId)
    {
        return _taskLogs.GetOrAdd(taskId, id => _taskLogDataStoreFactory.CreateLogDataStores());
    }
}
