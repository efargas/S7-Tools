using System;
using System.Collections.Concurrent;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

public class CentralizedTaskLogService : ICentralizedTaskLogService
{
    private readonly ConcurrentDictionary<Guid, (TaskLogDataStore main, TaskLogDataStore process, TaskLogDataStore protocol)> _taskLogs = new();
    private readonly ITaskLogDataStoreFactory _taskLogDataStoreFactory;

    public CentralizedTaskLogService(ITaskLogDataStoreFactory taskLogDataStoreFactory)
    {
        _taskLogDataStoreFactory = taskLogDataStoreFactory;
    }

    public (TaskLogDataStore main, TaskLogDataStore process, TaskLogDataStore protocol) GetOrCreateStoresForTask(Guid taskId)
    {
        return _taskLogs.GetOrAdd(taskId, id => _taskLogDataStoreFactory.CreateLogDataStores());
    }
}
