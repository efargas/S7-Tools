import re

with open('src/S7Tools/ViewModels/Components/TaskLogsPanelViewModel.cs', 'r') as f:
    content = f.read()

# Since ITaskLogDataStore inherits from IEnumerable<LogModel>, we can just use `store` as IEnumerable
# And for IUIThreadService, let's check what method it actually has (InvokeOnUIThreadAsync)

old_func = """    private void PopulateInitialLogEntries(ITaskLogDataStore? store, ObservableCollection<LogEntry> targetCollection)
    {
        if (store == null)
        {
            return;
        }

        var entries = store.GetEntries();
        int count = entries.Count();
        if (count == 0) return;

        var initialEntries = new List<LogEntry>();
        int skipCount = Math.Max(0, count - MaxLogEntries);

        // Access via Linq skip
        var logsToMap = entries.Skip(skipCount);
        foreach (var log in logsToMap)
        {
            initialEntries.Add(MapToLogEntry(log));
        }

        if (initialEntries.Count > 0)
        {
            _uiThreadService?.InvokeAsync(() =>
            {
                // Clear before adding if not empty to ensure clean state
                targetCollection.Clear();
                foreach (var entry in initialEntries)
                {
                    targetCollection.Add(entry);
                }
            });
        }
    }"""

new_func = """    private void PopulateInitialLogEntries(ITaskLogDataStore? store, ObservableCollection<LogEntry> targetCollection)
    {
        if (store == null)
        {
            return;
        }

        int count = store.Count();
        if (count == 0) return;

        var initialEntries = new List<LogEntry>();
        int skipCount = Math.Max(0, count - MaxLogEntries);

        // Access via Linq skip
        var logsToMap = store.Skip(skipCount);
        foreach (var log in logsToMap)
        {
            initialEntries.Add(MapToLogEntry(log));
        }

        if (initialEntries.Count > 0)
        {
            _uiThreadService?.InvokeOnUIThreadAsync(() =>
            {
                // Clear before adding if not empty to ensure clean state
                targetCollection.Clear();
                foreach (var entry in initialEntries)
                {
                    targetCollection.Add(entry);
                }
            });
        }
    }"""

content = content.replace(old_func, new_func)

with open('src/S7Tools/ViewModels/Components/TaskLogsPanelViewModel.cs', 'w') as f:
    f.write(content)
