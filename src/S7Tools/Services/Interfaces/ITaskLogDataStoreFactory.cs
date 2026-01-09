using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services.Interfaces;

public interface ITaskLogDataStoreFactory
{
    (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) CreateLogDataStores();
}
