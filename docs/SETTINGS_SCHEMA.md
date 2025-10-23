# S7Tools Settings Schema Documentation

**Last Updated**: October 23, 2025  
**Version**: 1.0

## Overview

S7Tools uses a key-value based settings system with dot notation for hierarchical organization. Settings are persisted in JSON format and managed through `ISettingsService`.

## Naming Convention

**Pattern**: `category.subcategory.setting`

- Use lowercase for consistency
- Use dots (`.`) for hierarchy
- Use descriptive, self-documenting names
- Group related settings under common prefixes

## Settings Categories

### 1. Logging Settings (`logging.*`)

Configuration for application logging system.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `logging.logDirectory` | `string` | `Resources/Logs/Main` | Directory for main log files |
| `logging.exportDirectory` | `string` | `Resources/Logs/Exported` | Directory for exported logs |
| `logging.level` | `string` | `Information` | Minimum log level (Trace, Debug, Information, Warning, Error, Critical) |
| `logging.enableFileLogging` | `bool` | `true` | Enable rolling file logging |

**Usage Example**:
```csharp
string logDir = _settingsService.GetSetting<string>("logging.logDirectory", _pathService.MainLogsDirectory);
```

---

### 2. UI Preferences (`ui.*`)

User interface behavior and display preferences.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `ui.autoScrollLogs` | `bool` | `true` | Auto-scroll log viewer to bottom on new entries |
| `ui.showTimestampInLogs` | `bool` | `true` | Display timestamp column in log viewer |
| `ui.showCategoryInLogs` | `bool` | `true` | Display category column in log viewer |
| `ui.showLogLevelInLogs` | `bool` | `true` | Display log level column in log viewer |

**Usage Example**:
```csharp
bool autoScroll = _settingsService.GetSetting<bool>("ui.autoScrollLogs", true);
```

---

### 3. Profile Paths (`profiles.*`)

File paths to profile configuration files.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `profiles.serialPath` | `string` | `Resources/Profiles/Serial/SerialProfiles.json` | Serial port profiles file |
| `profiles.socatPath` | `string` | `Resources/Profiles/Socat/SocatProfiles.json` | Socat bridge profiles file |
| `profiles.powerSupplyPath` | `string` | `Resources/Profiles/PowerSupply/PowerSupplyProfiles.json` | Power supply profiles file |

**Important**: These settings store **file paths**, not directory paths. ViewModels extract directories using `Path.GetDirectoryName()`.

**Usage Example**:
```csharp
string profilePath = _settingsService.GetSetting<string>("profiles.powerSupplyPath", _pathService.PowerSupplyProfilesPath);
string directory = Path.GetDirectoryName(profilePath);
```

---

### 4. Power Supply Settings (`powerSupply.*`)

Power supply specific configuration.

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `powerSupply.connectionTimeout` | `int` | `5000` | Connection timeout in milliseconds |
| `powerSupply.retryAttempts` | `int` | `3` | Number of retry attempts for operations |

**Usage Example**:
```csharp
int timeout = _settingsService.GetSetting<int>("powerSupply.connectionTimeout", 5000);
```

---

## Type Conventions

### String Settings
Use for:
- File and directory paths
- Enum-like values (log levels, modes)
- User-entered text

**Best Practice**: Always provide sensible defaults from `IPathService` or constants.

### Boolean Settings
Use for:
- Feature flags
- UI toggles
- Enable/disable switches

**Best Practice**: Default to the most user-friendly or safe option.

### Numeric Settings
Use for:
- Timeouts
- Counts and limits
- Numeric thresholds

**Best Practice**: Use appropriate types (`int`, `double`) and validate ranges.

---

## Adding New Settings

### 1. Choose the Right Category

Pick an existing category or create a new one:
- **Existing**: `logging.*`, `ui.*`, `profiles.*`, `powerSupply.*`
- **New**: Use a clear, descriptive category name

### 2. Define the Key

Follow the naming convention:
```
category.subcategory.setting
```

Examples:
- `serial.port.baudRate`
- `socat.network.timeout`
- `jobs.execution.maxParallel`

### 3. Document the Setting

Add to this file:
- Key name
- Data type
- Default value
- Clear description

### 4. Implement Usage

```csharp
// Get with default
var value = _settingsService.GetSetting<T>("category.setting", defaultValue);

// Set
await _settingsService.SetSettingAsync("category.setting", value).ConfigureAwait(false);

// Reset to default
await _settingsService.ResetSettingAsync("category.setting").ConfigureAwait(false);
```

---

## Settings Service API

### Reading Settings

```csharp
// Generic read with default
T GetSetting<T>(string key, T defaultValue);

// Check if setting exists
bool HasSetting(string key);
```

### Writing Settings

```csharp
// Set single value
Task SetSettingAsync(string key, object value);

// Save multiple settings atomically
Task SaveUserSettingsAsync(Dictionary<string, object> settings);
```

### Resetting Settings

```csharp
// Reset single setting to default
Task ResetSettingAsync(string key);

// Reset all settings
Task ResetAllSettingsAsync();
```

### Change Notifications

```csharp
// Subscribe to changes
event EventHandler<SettingsChangedEventArgs> SettingsChanged;

// EventArgs contains:
public class SettingsChangedEventArgs : EventArgs
{
    public string Key { get; init; }
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}
```

---

## Pattern: Settings-Dependent ViewModels

ViewModels that depend on settings should:

### 1. Subscribe to SettingsChanged

```csharp
private EventHandler<SettingsChangedEventArgs>? _settingsChangedHandler;

// In constructor
_settingsChangedHandler = (_, args) =>
{
    // Filter for relevant keys
    if (args.Key.StartsWith("powerSupply.") || args.Key.StartsWith("profiles.powerSupply"))
    {
        RefreshFromSettings();
    }
};
_settingsService.SettingsChanged += _settingsChangedHandler;
```

### 2. Implement RefreshFromSettings

```csharp
private void RefreshFromSettings()
{
    try
    {
        // Get setting with default
        string profilePath = _settingsService.GetSetting<string>(
            "profiles.powerSupplyPath", 
            _pathService.PowerSupplyProfilesPath);
        
        // Process and validate
        string? directory = Path.GetDirectoryName(profilePath);
        string resolved = _pathService.ResolvePath(directory ?? string.Empty);
        
        // Apply with fallback
        if (!string.IsNullOrEmpty(resolved) && Directory.Exists(resolved))
        {
            ProfilesPath = resolved;
        }
        else
        {
            ProfilesPath = Path.GetDirectoryName(_pathService.PowerSupplyProfilesPath) 
                ?? _pathService.ProfilesDirectory;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to refresh settings");
        ProfilesPath = _pathService.ProfilesDirectory; // Safe fallback
    }
}
```

### 3. Unsubscribe in Dispose

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        if (_settingsChangedHandler != null)
        {
            _settingsService.SettingsChanged -= _settingsChangedHandler;
        }
    }
    base.Dispose(disposing);
}
```

---

## Migration and Versioning

### Adding New Settings
- New settings default gracefully if not in user config
- No migration required for additions

### Renaming Settings
- Add new setting
- Keep old setting for one version
- Migrate in code during load
- Document deprecation

### Removing Settings
- Mark as deprecated for one version
- Remove in subsequent version
- Document in changelog

### Type Changes
- Avoid changing types of existing settings
- If necessary, create new setting with different key
- Migrate data during load

---

## Best Practices

### ✅ DO
- Use descriptive, self-documenting key names
- Provide sensible defaults from `IPathService` or constants
- Validate settings values when reading
- Use appropriate data types (avoid storing complex objects)
- Subscribe to `SettingsChanged` for dynamic updates
- Unsubscribe event handlers in Dispose
- Document all settings in this file

### ❌ DON'T
- Store complex objects in settings (use separate config files)
- Use string interpolation for keys (use constants)
- Forget to provide defaults
- Mix camelCase and snake_case (use dot notation)
- Subscribe to SettingsChanged without unsubscribing
- Modify settings on UI thread without async
- Assume settings always exist (always provide defaults)

---

## See Also

- **Implementation**: `src/S7Tools/Services/SettingsService.cs`
- **Interface**: `src/S7Tools.Core/Interfaces/Services/ISettingsService.cs`
- **Path Service**: `src/S7Tools.Core/Interfaces/Services/IPathService.cs`
- **Architecture**: `.copilot-tracking/memory-bank/systemPatterns.md` (Section 4.5)
- **Examples**: See ViewModels: `PowerSupplySettingsViewModel`, `LoggingSettingsViewModel`

---

**Maintainers**: Update this document when adding, changing, or removing settings.  
**Developers**: Consult this document before creating new settings.
