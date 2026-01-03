using System;
using S7Tools.Infrastructure.Logging.Core.Storage;

namespace S7Tools.Services.Interfaces;

public interface ICentralizedTaskLogService
{
    (TaskLogDataStore main, TaskLogDataStore process, TaskLogDataStore protocol) GetOrCreateStoresForTask(Guid taskId);
}
