# TASK005: Profile Management Improvements & Missing Features

**Created**: 2025-10-10  
**Priority**: HIGH  
**Status**: PENDING  
**Estimated Effort**: 3-5 days  

---

## Overview

This task addresses remaining issues and missing features in the profile management system after resolving the semaphore deadlock (BUG001). Multiple areas require attention to achieve feature parity and improve user experience.

---

## Issues to Address

### 1. ❌ Socat Start Not Working

**Problem**: Socat process start functionality is not operational

**Current State**:
- Start button exists in UI
- Command is bound but execution fails or does nothing
- No error messages visible to user

**Investigation Needed**:
- Check `SocatService.StartSocatWithProfileAsync()` implementation
- Verify socat command generation
- Check process spawning and monitoring
- Review error handling and logging

**Files to Review**:
- `src/S7Tools/Services/SocatService.cs`
- `src/S7Tools/ViewModels/SocatSettingsViewModel.cs`
- `src/S7Tools/Views/SocatSettingsView.axaml`

**Acceptance Criteria**:
- [ ] Start button successfully launches socat process
- [ ] Process appears in running processes list
- [ ] Error messages displayed if start fails
- [ ] Proper logging of start operations
- [ ] Process monitoring works correctly

---

### 2. ❌ Socat Process Management Not Implemented

**Problem**: Search, kill, and process management operations are incomplete

**Missing Features**:
- Search/filter running socat processes
- Kill individual process
- Kill all processes
- Process status monitoring
- Connection count tracking

**Current State**:
- `GetRunningProcessesAsync()` may be incomplete
- Stop commands may not work
- No process refresh mechanism

**Files to Review**:
- `src/S7Tools/Services/SocatService.cs`
- `src/S7Tools/ViewModels/SocatSettingsViewModel.cs`
- `src/S7Tools/Core/Models/SocatProcessInfo.cs`

**Implementation Requirements**:
```csharp
// Required methods in ISocatService
Task<IEnumerable<SocatProcessInfo>> GetRunningProcessesAsync();
Task<bool> StopSocatAsync(SocatProcessInfo process);
Task<int> StopAllSocatProcessesAsync();
Task<bool> TestTcpConnectionAsync(string host, int port);

// Process monitoring
event EventHandler<SocatProcessEventArgs> ProcessStarted;
event EventHandler<SocatProcessEventArgs> ProcessStopped;
event EventHandler<SocatProcessEventArgs> ProcessError;
```

**Acceptance Criteria**:
- [ ] Can list all running socat processes
- [ ] Can stop individual process by PID
- [ ] Can stop all processes at once
- [ ] Process list updates in real-time
- [ ] Connection count tracking works
- [ ] Test connection functionality works

---

### 3. ❌ Socat Import Not Implemented

**Problem**: Import functionality shows placeholder dialog

**Current State**:
```csharp
private async Task ImportProfilesAsync()
{
    await _dialogService.ShowErrorAsync("Import Profiles",
        "Import functionality will be implemented in the UI layer.");
}
```

**Required Implementation**:
- File dialog to select JSON file
- Support both single profile and array formats
- Validation of imported data
- Conflict resolution (overwrite vs skip)
- User feedback on import results

**Files to Modify**:
- `src/S7Tools/ViewModels/SocatSettingsViewModel.cs`
- `src/S7Tools/Services/SocatProfileService.cs` (already has import methods)

**Implementation Pattern** (copy from Serial):
```csharp
private async Task ImportProfilesAsync()
{
    if (_fileDialogService == null)
    {
        StatusMessage = "File dialog service not available";
        return;
    }

    try
    {
        var fileName = await _fileDialogService.ShowOpenFileDialogAsync(
            "Import Profiles",
            "JSON files (*.json)|*.json|All files (*.*)|*.*");

        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Importing profiles...";

        var jsonData = await File.ReadAllTextAsync(fileName);
        var importedProfiles = await _profileService.ImportProfilesFromJsonAsync(jsonData, overwriteExisting: false);

        await RefreshProfilesPreserveSelectionAsync(null);

        StatusMessage = $"Imported {importedProfiles.Count()} profile(s) from {Path.GetFileName(fileName)}";
        _logger.LogInformation("Imported {Count} profiles from {FileName}", importedProfiles.Count(), fileName);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error importing profiles");
        StatusMessage = $"Error importing profiles: {ex.Message}";
        await _dialogService.ShowErrorAsync("Import Error", ex.Message);
    }
    finally
    {
        IsLoading = false;
    }
}
```

**Acceptance Criteria**:
- [ ] File dialog opens and allows JSON selection
- [ ] Handles both single profile and array formats
- [ ] Shows conflict resolution dialog if needed
- [ ] Displays success/error messages
- [ ] Refreshes profile list after import
- [ ] Proper error handling and logging

---

### 4. ❌ Serial Import Fails for Single Profile

**Problem**: Import expects array format, fails when importing single profile

**Error from Logs**:
```json
{
  "level": "Error",
  "message": "Error importing profiles",
  "exception": "System.ArgumentException: Invalid JSON data: The JSON value could not be converted to System.Collections.Generic.List<SerialPortProfile>"
}
```

**Root Cause**:
- `ImportProfilesFromJsonAsync` tries to deserialize as `List<Profile>`
- Single profile export creates object, not array
- No fallback to try single profile deserialization

**Current Code** (`SerialPortProfileService.cs` line 552):
```csharp
public async Task<IEnumerable<SerialPortProfile>> ImportProfilesFromJsonAsync(string jsonData, ...)
{
    try
    {
        importedProfiles = JsonSerializer.Deserialize<List<SerialPortProfile>>(jsonData, options)
            ?? throw new InvalidOperationException("Failed to deserialize profiles from JSON");
    }
    catch (JsonException ex)
    {
        throw new ArgumentException($"Invalid JSON data: {ex.Message}", nameof(jsonData), ex);
    }
    // ...
}
```

**Required Fix**:
```csharp
public async Task<IEnumerable<SerialPortProfile>> ImportProfilesFromJsonAsync(string jsonData, ...)
{
    List<SerialPortProfile> importedProfiles;
    
    try
    {
        // Try to deserialize as array first
        importedProfiles = JsonSerializer.Deserialize<List<SerialPortProfile>>(jsonData, options);
        
        if (importedProfiles != null)
        {
            _logger.LogDebug("Deserialized as profile array: {Count} profiles", importedProfiles.Count);
        }
    }
    catch (JsonException)
    {
        // If array deserialization fails, try single profile
        try
        {
            var singleProfile = JsonSerializer.Deserialize<SerialPortProfile>(jsonData, options);
            if (singleProfile != null)
            {
                importedProfiles = new List<SerialPortProfile> { singleProfile };
                _logger.LogDebug("Deserialized as single profile: {ProfileName}", singleProfile.Name);
            }
            else
            {
                throw new ArgumentException("Failed to deserialize profile from JSON", nameof(jsonData));
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON data: Could not deserialize as profile or profile array. {ex.Message}", nameof(jsonData), ex);
        }
    }
    
    // Continue with existing logic...
}
```

**Files to Modify**:
- `src/S7Tools/Services/SerialPortProfileService.cs` (line ~552)
- `src/S7Tools/Services/SocatProfileService.cs` (same issue likely exists)

**Acceptance Criteria**:
- [ ] Can import single profile JSON
- [ ] Can import array of profiles JSON
- [ ] Clear error message if JSON is invalid
- [ ] Proper logging of import type detected
- [ ] Apply fix to both Serial and Socat services

---

### 5. ❌ Duplicate "Open Folder" Implementation

**Problem**: "Open folder" functionality is duplicated across multiple ViewModels

**Current Locations**:
- `SerialPortsSettingsViewModel.OpenProfilesPathAsync()`
- `SocatSettingsViewModel.OpenProfilesPathAsync()`
- `LoggingSettingsViewModel.OpenSettingsDirectoryAsync()` (possibly)

**Issues**:
- Code duplication (DRY violation)
- Inconsistent error handling
- Platform-specific logic repeated
- Difficult to maintain

**Current Implementation** (duplicated):
```csharp
private async Task OpenProfilesPathAsync()
{
    try
    {
        var absolutePath = Path.IsPathRooted(ProfilesPath) ? ProfilesPath :
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProfilesPath);

        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
        }

        await Task.Run(() =>
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start("explorer.exe", absolutePath);
            }
            else if (OperatingSystem.IsLinux())
            {
                var fileManagers = new[] { "xdg-open", "nautilus", "dolphin", "thunar", "pcmanfm" };
                // ... try each file manager
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", absolutePath);
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error opening folder");
    }
}
```

**Proposed Solution**: Create reusable service

**New Interface**:
```csharp
namespace S7Tools.Services.Interfaces;

public interface IFileSystemService
{
    /// <summary>
    /// Opens a folder in the system's default file explorer.
    /// Creates the folder if it doesn't exist.
    /// </summary>
    /// <param name="path">Absolute or relative path to open</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> OpenFolderAsync(string path);
    
    /// <summary>
    /// Resolves a relative path to an absolute path based on application directory.
    /// </summary>
    string GetAbsolutePath(string path);
    
    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    Task EnsureDirectoryExistsAsync(string path);
}
```

**Implementation**:
```csharp
namespace S7Tools.Services;

public class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;
    
    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public string GetAbsolutePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }
        
        return Path.IsPathRooted(path) 
            ? path 
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
    }
    
    public async Task EnsureDirectoryExistsAsync(string path)
    {
        var absolutePath = GetAbsolutePath(path);
        
        await Task.Run(() =>
        {
            if (!Directory.Exists(absolutePath))
            {
                Directory.CreateDirectory(absolutePath);
                _logger.LogInformation("Created directory: {Path}", absolutePath);
            }
        });
    }
    
    public async Task<bool> OpenFolderAsync(string path)
    {
        try
        {
            var absolutePath = GetAbsolutePath(path);
            
            await EnsureDirectoryExistsAsync(absolutePath);
            
            _logger.LogInformation("Opening folder: {Path}", absolutePath);
            
            await Task.Run(() =>
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start("explorer.exe", absolutePath);
                }
                else if (OperatingSystem.IsLinux())
                {
                    if (!TryOpenWithLinuxFileManager(absolutePath))
                    {
                        throw new PlatformNotSupportedException("No suitable file manager found on Linux");
                    }
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", absolutePath);
                }
                else
                {
                    throw new PlatformNotSupportedException($"Opening folders is not supported on this platform");
                }
            });
            
            _logger.LogInformation("Successfully opened folder");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder: {Path}", path);
            return false;
        }
    }
    
    private bool TryOpenWithLinuxFileManager(string path)
    {
        var fileManagers = new[] { "xdg-open", "nautilus", "dolphin", "thunar", "pcmanfm" };
        
        foreach (var fileManager in fileManagers)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileManager,
                        Arguments = path,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                
                if (process.Start())
                {
                    _logger.LogDebug("Opened folder with {FileManager}", fileManager);
                    return true;
                }
            }
            catch
            {
                // Try next file manager
                continue;
            }
        }
        
        return false;
    }
}
```

**Files to Create**:
- `src/S7Tools.Core/Services/Interfaces/IFileSystemService.cs`
- `src/S7Tools/Services/FileSystemService.cs`

**Files to Modify**:
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` (register service)
- `src/S7Tools/ViewModels/SerialPortsSettingsViewModel.cs` (use service)
- `src/S7Tools/ViewModels/SocatSettingsViewModel.cs` (use service)
- `src/S7Tools/ViewModels/LoggingSettingsViewModel.cs` (use service if applicable)

**Usage in ViewModels**:
```csharp
public class SocatSettingsViewModel : ViewModelBase
{
    private readonly IFileSystemService _fileSystemService;
    
    public SocatSettingsViewModel(IFileSystemService fileSystemService, ...)
    {
        _fileSystemService = fileSystemService;
    }
    
    private async Task OpenProfilesPathAsync()
    {
        StatusMessage = "Opening profiles folder...";
        
        var success = await _fileSystemService.OpenFolderAsync(ProfilesPath);
        
        StatusMessage = success 
            ? "Profiles folder opened" 
            : "Error opening profiles folder";
    }
}
```

**Acceptance Criteria**:
- [ ] Create `IFileSystemService` interface
- [ ] Implement `FileSystemService` with platform detection
- [ ] Register service in DI container
- [ ] Replace all duplicate implementations with service calls
- [ ] Test on Windows, Linux, and macOS
- [ ] Proper error handling and logging
- [ ] Consistent user feedback across all views

---

## Implementation Order

1. **Fix Serial Import (Single Profile)** - Quick win, affects user experience
2. **Implement Socat Import** - Copy working pattern from Serial
3. **Create FileSystemService** - Refactoring, improves maintainability
4. **Fix Socat Start** - Core functionality, requires investigation
5. **Implement Process Management** - Depends on Socat Start working

---

## Testing Requirements

### Unit Tests
- [ ] Test import with single profile JSON
- [ ] Test import with array JSON
- [ ] Test import with invalid JSON
- [ ] Test FileSystemService on all platforms
- [ ] Test process start/stop operations

### Integration Tests
- [ ] Test full import workflow (Serial and Socat)
- [ ] Test socat process lifecycle
- [ ] Test profile CRUD with file system operations

### Manual Testing
- [ ] Import single profile (Serial and Socat)
- [ ] Import multiple profiles (Serial and Socat)
- [ ] Start socat process with profile
- [ ] Stop individual process
- [ ] Stop all processes
- [ ] Open folder on Windows/Linux/macOS
- [ ] Navigate between views (verify persistence)

---

## Files Summary

### To Create
- `src/S7Tools.Core/Services/Interfaces/IFileSystemService.cs`
- `src/S7Tools/Services/FileSystemService.cs`

### To Modify
- `src/S7Tools/Services/SerialPortProfileService.cs` (import fix)
- `src/S7Tools/Services/SocatProfileService.cs` (import fix)
- `src/S7Tools/Services/SocatService.cs` (start/stop implementation)
- `src/S7Tools/ViewModels/SerialPortsSettingsViewModel.cs` (use FileSystemService, import)
- `src/S7Tools/ViewModels/SocatSettingsViewModel.cs` (use FileSystemService, import, start/stop)
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` (register FileSystemService)

### To Review
- `src/S7Tools/Core/Models/SocatProcessInfo.cs`
- `src/S7Tools/Views/SocatSettingsView.axaml`

---

## Dependencies

- No external package dependencies required
- Requires BUG001 fix to be completed (semaphore deadlock)
- FileSystemService should be implemented before refactoring ViewModels

---

## Success Criteria

- [ ] All import operations work for both single and array formats
- [ ] Socat processes can be started, stopped, and monitored
- [ ] No code duplication for file system operations
- [ ] All operations have proper error handling
- [ ] Comprehensive logging for debugging
- [ ] User receives clear feedback for all operations
- [ ] Profile persistence works across view navigation
- [ ] All tests pass

---

## Notes

- Consider adding progress indicators for long-running operations
- May want to add confirmation dialogs for destructive operations (stop all processes)
- Consider adding profile validation before import
- FileSystemService could be extended for other file operations in the future
- Process monitoring may benefit from background service pattern

---

**Task Owner**: TBD  
**Reviewer**: TBD  
**Target Completion**: TBD
