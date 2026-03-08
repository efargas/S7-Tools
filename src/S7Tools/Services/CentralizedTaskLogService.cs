using System;
using System.Collections.Concurrent;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

public class CentralizedTaskLogService(ITaskLogDataStoreFactory taskLogDataStoreFactory) : ICentralizedTaskLogService
{
    private readonly ConcurrentDictionary<Guid, (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog)> _taskLogs = new();
    private readonly ITaskLogDataStoreFactory _taskLogDataStoreFactory = taskLogDataStoreFactory ?? throw new ArgumentNullException(nameof(taskLogDataStoreFactory));

    public (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) GetOrCreateStoresForTask(Guid taskId)
    {
        return _taskLogs.GetOrAdd(taskId, id => _taskLogDataStoreFactory.CreateLogDataStores());
    }
}
