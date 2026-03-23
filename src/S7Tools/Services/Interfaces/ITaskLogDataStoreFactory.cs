using S7Tools.Core.Interfaces.Services;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Represents the ITaskLogDataStoreFactory.
/// </summary>
public interface ITaskLogDataStoreFactory
{
    (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) CreateLogDataStores();
}
