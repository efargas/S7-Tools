using System;

namespace S7Tools.Core.Services.Interfaces;

public interface ICentralizedTaskLogService
{
    (ITaskLogDataStore main, ITaskLogDataStore process, ITaskLogDataStore protocol) GetOrCreateStoresForTask(Guid taskId);
}
