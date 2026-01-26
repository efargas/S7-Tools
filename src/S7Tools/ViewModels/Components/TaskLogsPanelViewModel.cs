    private void InitializeLogs()
    {
        if (_task == null || _task.TaskId == Guid.Empty)
            return;

        // Get persistent stores for the task
        (_mainLogDataStore, _processLogDataStore, _) = _centralizedTaskLogService.GetOrCreateStoresForTask(_task.TaskId);

        // Populate initial
        if (_mainLogDataStore != null)
        {
            var initialMainLogs = _mainLogDataStore.Skip(Math.Max(0, _mainLogDataStore.Count() - MaxUiLogEntries));
            foreach (S7Tools.Core.Models.LogModel logModel in initialMainLogs)
            {
                MainLogEntries.Add(MapToLogEntry(logModel));
            }
            _mainLogDataStore.CollectionChanged += _mainHandler;
        }

        if (_processLogDataStore != null)
        {
            var initialProcessLogs = _processLogDataStore.Skip(Math.Max(0, _processLogDataStore.Count() - MaxUiLogEntries));
            foreach (S7Tools.Core.Models.LogModel logModel in initialProcessLogs)
            {
                ProcessLogEntries.Add(MapToLogEntry(logModel));
            }
            _processLogDataStore.CollectionChanged += _processHandler;
        }
    }
