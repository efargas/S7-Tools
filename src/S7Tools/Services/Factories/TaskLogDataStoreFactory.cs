using Microsoft.Extensions.Options;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

public class TaskLogDataStoreFactory(IOptions<TaskLogDataStoreOptions> options) : ITaskLogDataStoreFactory
{
    private readonly IOptions<TaskLogDataStoreOptions> _options = options ?? throw new ArgumentNullException(nameof(options));

    public (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) CreateLogDataStores()
    {
        return (
            MainLog: new S7Tools.Infrastructure.Logging.Core.Storage.LogDataStore(new S7Tools.Infrastructure.Logging.Core.Models.LogDataStoreOptions { MaxEntries = _options.Value.MaxEntries }),
            ProcessLog: new S7Tools.Infrastructure.Logging.Core.Storage.LogDataStore(new S7Tools.Infrastructure.Logging.Core.Models.LogDataStoreOptions { MaxEntries = _options.Value.MaxEntries }),
            ProtocolLog: new S7Tools.Infrastructure.Logging.Core.Storage.LogDataStore(new S7Tools.Infrastructure.Logging.Core.Models.LogDataStoreOptions { MaxEntries = _options.Value.MaxEntries })
        );
    }
}
