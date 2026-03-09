import re

with open('src/S7Tools/ViewModels/Components/TaskLogsPanelViewModel.cs', 'r') as f:
    content = f.read()

# Fix the Count and indexers since ITaskLogDataStore is likely lacking .Count property, it should be Count() maybe?
# and indexing `store[i]` shouldn't be used if it's an IEnumerable, maybe `store.GetEntries().Skip(skipCount)`?
# Let's search for ITaskLogDataStore.

old_method = """    public async Task InitializeFromStoreAsync(ITaskLogDataStore store)
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

new_method = """    public async Task InitializeFromStoreAsync(ITaskLogDataStore store)
    {
        if (store == null)
        {
            return;
        }

        var entries = store.GetEntries();
        int count = entries.Count();
        if (count == 0) return;

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

content = content.replace(old_method, new_method)

with open('src/S7Tools/ViewModels/Components/TaskLogsPanelViewModel.cs', 'w') as f:
    f.write(content)

with open('src/S7Tools/Services/Bootloader/EnhancedBootloaderService.cs', 'r') as f:
    content2 = f.read()

# BootloaderResult.Data vs Result property or similar.
# CS1061: 'BootloaderResult' does not contain a definition for 'Data'
content2 = content2.replace("result.Data", "result.ResponseData") # Usually it's ResponseData or Payload or Result. I'll try guessing ResponseData for now or skip it if I shouldn't guess. Actually it's best to grep it first.
