using S7Tools.Infrastructure.Logging.Core.Storage;

namespace S7Tools.Services.Interfaces;

public interface ITaskLogDataStoreFactory
{
    (TaskLogDataStore main, TaskLogDataStore process, TaskLogDataStore protocol) CreateLogDataStores();
}
