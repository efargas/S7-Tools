using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Interfaces;

public interface ITaskLogDataStoreFactory
{
    (ITaskLogDataStore main, ITaskLogDataStore process, ITaskLogDataStore protocol) CreateLogDataStores();
}
