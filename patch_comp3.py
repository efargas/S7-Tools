import re

# Fixing the remaining errors in TaskLogsPanelViewModel
with open('src/S7Tools/ViewModels/Components/TaskLogsPanelViewModel.cs', 'r') as f:
    content = f.read()

old_func = """    public async Task InitializeFromStoreAsync(ITaskLogDataStore store)
    {
        if (store == null || store.Count == 0)
        {
            return;
        }

        // Get recent entries up to max
        int skipCount = Math.Max(0, store.Count - MaxLogEntries);
        var initialEntries = new List<TaskLogEntry>();

        for (int i = skipCount; i < store.Count; i++)
        {
            initialEntries.Add(store[i]);
        }

        if (initialEntries.Count > 0)
        {
            await _uiThread.Post(() =>
            {
                MainLogEntries.Clear();
                foreach (var entry in initialEntries)
                {
                    MainLogEntries.Add(entry);
                }
            });
        }
    }"""

new_func = """    public async Task InitializeFromStoreAsync(ITaskLogDataStore store)
    {
        if (store == null)
        {
            return;
        }

        var entries = store.GetEntries();
        int count = entries.Count();
        if (count == 0)
        {
            return;
        }

        // Get recent entries up to max
        int skipCount = Math.Max(0, count - MaxLogEntries);
        var initialEntries = entries.Skip(skipCount).ToList();

        if (initialEntries.Count > 0)
        {
            await _uiThread.InvokeAsync(() =>
            {
                MainLogEntries.Clear();
                foreach (var entry in initialEntries)
                {
                    MainLogEntries.Add(entry);
                }
            });
        }
    }"""

content = content.replace(old_func, new_func)

with open('src/S7Tools/ViewModels/Components/TaskLogsPanelViewModel.cs', 'w') as f:
    f.write(content)
