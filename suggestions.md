## PR Reviewer Guide 🔍

Here are some key observations to aid the review process:

<table>
<tr><td>⏱️&nbsp;<strong>Estimated effort to review</strong>: 4 🔵🔵🔵🔵⚪</td></tr>
<tr><td>🧪&nbsp;<strong>PR contains tests</strong></td></tr>
<tr><td>🔒&nbsp;<strong>No security concerns identified</strong></td></tr>
<tr><td>⚡&nbsp;<strong>Recommended focus areas for review</strong><br><br>

<details><summary><a href='https://github.com/efargas/S7-Tools/pull/67/files#diff-2ac3c5cf4a354c726bb29713925111d4af116db73d59e6bf4d2d1ef35198622dR102-R113'><strong>Event Handler Cleanup</strong></a>

The view model subscribes to `_settingsService.SettingsChanged` with `_settingsChangedHandler` but there is no visible unsubscription on disposal. Verify disposal/cleanup to prevent memory leaks.
</summary>

```c#

// Initialize ProfilesPath from settings and subscribe to changes
RefreshFromSettings();
_settingsChangedHandler = (_, args) =>
{
    if (args.Key.StartsWith("powerSupply.") || args.Key.StartsWith("profiles.powerSupply"))
    {
        RefreshFromSettings();
    }
};
_settingsService.SettingsChanged += _settingsChangedHandler;

```

</details>

<details><summary><a href='https://github.com/efargas/S7-Tools/pull/67/files#diff-2ac3c5cf4a354c726bb29713925111d4af116db73d59e6bf4d2d1ef35198622dR434-R453'><strong>Path Resolution Robustness</strong></a>

`RefreshFromSettings` derives a directory from a setting that appears to represent a file path, then resolves it; fallback sets `ProfilesPath` to `_pathService.ProfilesDirectory`. Confirm consistency of using file vs. directory paths and ensure `_pathService.ProfilesDirectory` matches the expected profiles type (power supply) to avoid misdirected paths.
</summary>

```c#
{
    try
    {
        // Use the new settings service with key-value access
        string powerSupplyProfilePath = _settingsService.GetSetting<string>("profiles.powerSupplyPath", _pathService.PowerSupplyProfilesPath);
        string? directoryPath = Path.GetDirectoryName(powerSupplyProfilePath);

        // Resolve the path using the path service, which handles both absolute and relative paths
        string resolvedPath = _pathService.ResolvePath(directoryPath ?? string.Empty);

        // If resolution results in an invalid path, fall back to the default profiles directory
        if (string.IsNullOrEmpty(resolvedPath) || !Directory.Exists(resolvedPath))
        {
            ProfilesPath = _pathService.ProfilesDirectory;
        }
        else
        {
            ProfilesPath = resolvedPath;
        }
    }

```

</details>

<details><summary><a href='https://github.com/efargas/S7-Tools/pull/67/files#diff-292462df6ef86c929e48ded271c72dc4522eadb52a9545294acd9541e8005318R153-R173'><strong>Settings File Path Placeholder</strong></a>

`CurrentSettingsFilePath` is hard-coded to `"Resources/AppSettings/AppSettings.json"` with a TODO-style comment. This may break opening settings folder or last-modified display. Replace with a value provided by the new settings service.
</summary>

```c#

private void RefreshFromSettings()
{
    // Load settings using the new structured approach
    DefaultLogPath = _settingsService.GetSetting<string>("logging.logDirectory", "Resources/Logs/Main");
    ExportPath = _settingsService.GetSetting<string>("logging.exportDirectory", "Resources/Logs/Exported");
    MinimumLogLevel = _settingsService.GetSetting<string>("logging.level", "Information");
    AutoScrollLogs = _settingsService.GetSetting<bool>("ui.autoScrollLogs", true);
    EnableRollingLogs = _settingsService.GetSetting<bool>("logging.enableFileLogging", true);
    ShowTimestampInLogs = _settingsService.GetSetting<bool>("ui.showTimestampInLogs", true);
    ShowCategoryInLogs = _settingsService.GetSetting<bool>("ui.showCategoryInLogs", true);
    ShowLogLevelInLogs = _settingsService.GetSetting<bool>("ui.showLogLevelInLogs", true);

    // For now, use a placeholder for settings file path - we need to add this to the settings service
    CurrentSettingsFilePath = "Resources/AppSettings/AppSettings.json";

    try
    {
        var fileInfo = new System.IO.FileInfo(CurrentSettingsFilePath);
        SettingsLastModified = fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now;
    }

```

</details>

</td></tr>
</table>
