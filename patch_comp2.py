import re

# Fix CS1061 BootloaderResult.Data
# Let's replace result.Data with result.SavedFiles

with open('src/S7Tools/Services/Bootloader/EnhancedBootloaderService.cs', 'r') as f:
    content = f.read()

content = content.replace("result.Data", "result.SavedFiles")

with open('src/S7Tools/Services/Bootloader/EnhancedBootloaderService.cs', 'w') as f:
    f.write(content)

with open('src/S7Tools/ViewModels/Tasks/TaskDetailsViewModel.cs', 'r') as f:
    content = f.read()

content = content.replace("result.Data", "result.SavedFiles")

with open('src/S7Tools/ViewModels/Tasks/TaskDetailsViewModel.cs', 'w') as f:
    f.write(content)

with open('src/S7Tools/Services/Tasking/JobScheduler.cs', 'r') as f:
    content = f.read()

content = content.replace("result.Data", "result.SavedFiles")

with open('src/S7Tools/Services/Tasking/JobScheduler.cs', 'w') as f:
    f.write(content)

with open('src/S7Tools/Services/Tasking/EnhancedTaskScheduler.cs', 'r') as f:
    content = f.read()

content = content.replace("result.Data", "result.SavedFiles")

with open('src/S7Tools/Services/Tasking/EnhancedTaskScheduler.cs', 'w') as f:
    f.write(content)
