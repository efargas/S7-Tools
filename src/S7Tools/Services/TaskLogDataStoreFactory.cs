using System;
using Microsoft.Extensions.Options;
using S7Tools.Infrastructure.Logging.Core.Configuration;
using S7Tools.Infrastructure.Logging.Core.Storage;
using S7Tools.Services.Interfaces;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.Services;

public class TaskLogDataStoreFactory : ITaskLogDataStoreFactory
{
    private readonly IOptions<TaskLogDataStoreOptions> _options;
    private readonly IUIThreadService _uiThreadService;

    public TaskLogDataStoreFactory(IOptions<TaskLogDataStoreOptions> options, IUIThreadService uiThreadService)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _uiThreadService = uiThreadService ?? throw new ArgumentNullException(nameof(uiThreadService));
    }

    public (ITaskLogDataStore MainLog, ITaskLogDataStore ProcessLog, ITaskLogDataStore ProtocolLog) CreateLogDataStores()
    {
        // Use the dedicated TaskLogDataStore which is now optimized for this purpose
        // Inject the UI dispatch delegate
        Action<Action> dispatch = action => _uiThreadService.PostToUIThread(action);

        return (
            MainLog: new TaskLogDataStore(_options.Value.MaxEntries, dispatch),
            ProcessLog: new TaskLogDataStore(_options.Value.MaxEntries, dispatch),
            ProtocolLog: new TaskLogDataStore(_options.Value.MaxEntries, dispatch)
        );
    }
}
