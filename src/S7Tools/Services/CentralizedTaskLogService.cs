using System;
using System.Collections.Concurrent;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

public class CentralizedTaskLogService : ICentralizedTaskLogService
{
    private readonly ConcurrentDictionary<Guid, (ITaskLogDataStore main, ITaskLogDataStore process, ITaskLogDataStore protocol)> _taskLogs = new();
    private readonly ITaskLogDataStoreFactory _taskLogDataStoreFactory;

    public CentralizedTaskLogService(ITaskLogDataStoreFactory taskLogDataStoreFactory)
    {
        _taskLogDataStoreFactory = taskLogDataStoreFactory;
    }

    public (ITaskLogDataStore main, ITaskLogDataStore process, ITaskLogDataStore protocol) GetOrCreateStoresForTask(Guid taskId)
    {
        return _taskLogs.GetOrAdd(taskId, id => _taskLogDataStoreFactory.CreateLogDataStores());
    }
}
