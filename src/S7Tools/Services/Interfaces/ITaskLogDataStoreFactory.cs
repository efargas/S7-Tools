using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Services.Interfaces;

public interface ITaskLogDataStoreFactory
{
    (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) CreateLogDataStores();
}
