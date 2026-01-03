using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

public class TaskLogDataStoreFactory : ITaskLogDataStoreFactory
{
    private readonly IOptions<TaskLogDataStoreOptions> _options;

    public TaskLogDataStoreFactory(IOptions<TaskLogDataStoreOptions> options)
    {
        _options = options;
    }

    public (TaskLogDataStore main, TaskLogDataStore process, TaskLogDataStore protocol) CreateLogDataStores()
    {
        return (new TaskLogDataStore(_options.Value.MaxEntries), new TaskLogDataStore(_options.Value.MaxEntries), new TaskLogDataStore(_options.Value.MaxEntries));
    }
}
