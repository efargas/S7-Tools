using System.Globalization;
using S7Tools.Core.Resources;

namespace S7Tools.Resources;

/// <summary>
/// Strongly-typed resource class for UI strings.
/// </summary>
public static class UIStrings
{
    private static IResourceManager? _resourceManager;

    /// <summary>
    /// Gets or sets the resource manager for UI strings.
    /// </summary>
    public static IResourceManager ResourceManager
    {
        get => _resourceManager ?? throw new InvalidOperationException("ResourceManager not initialized. Ensure App.Initialize() has been called.");
        set => _resourceManager = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets a value indicating whether the ResourceManager is initialized.
    /// </summary>
    public static bool IsInitialized => _resourceManager != null;

    #region Application Strings

    /// <summary>
    /// Gets the application title.
    /// </summary>
    public static string ApplicationTitle => ResourceManager.GetString("ApplicationTitle") ?? "S7Tools";

    /// <summary>
    /// Gets the application description.
    /// </summary>
    public static string ApplicationDescription => ResourceManager.GetString("ApplicationDescription") ??
        "Cross-platform desktop application for Siemens S7-1200 PLC communication";

    /// <summary>
    /// Gets the application version text.
    /// </summary>
    public static string Version => ResourceManager.GetString("Version") ?? "Version";

    #endregion

    #region Menu Strings

    /// <summary>
    /// Gets the File menu text.
    /// </summary>
    public static string MenuFile => ResourceManager.GetString("MenuFile") ?? "File";

    /// <summary>
    /// Gets the Edit menu text.
    /// </summary>
    public static string MenuEdit => ResourceManager.GetString("MenuEdit") ?? "Edit";

    /// <summary>
    /// Gets the View menu text.
    /// </summary>
    public static string MenuView => ResourceManager.GetString("MenuView") ?? "View";

    /// <summary>
    /// Gets the Help menu text.
    /// </summary>
    public static string MenuHelp => ResourceManager.GetString("MenuHelp") ?? "Help";

    /// <summary>
    /// Gets the New File menu item text.
    /// </summary>
    public static string MenuNewFile => ResourceManager.GetString("MenuNewFile") ?? "New File";

    /// <summary>
    /// Gets the Open File menu item text.
    /// </summary>
    public static string MenuOpenFile => ResourceManager.GetString("MenuOpenFile") ?? "Open File...";

    /// <summary>
    /// Gets the Save menu item text.
    /// </summary>
    public static string MenuSave => ResourceManager.GetString("MenuSave") ?? "Save";

    /// <summary>
    /// Gets the Save As menu item text.
    /// </summary>
    public static string MenuSaveAs => ResourceManager.GetString("MenuSaveAs") ?? "Save As...";

    /// <summary>
    /// Gets the Exit menu item text.
    /// </summary>
    public static string MenuExit => ResourceManager.GetString("MenuExit") ?? "Exit";

    #endregion

    #region Activity Bar Strings

    /// <summary>
    /// Gets the Explorer activity bar item text.
    /// </summary>
    public static string ActivityBarExplorer => ResourceManager.GetString("ActivityBarExplorer") ?? "Explorer";

    /// <summary>
    /// Gets the Connections activity bar item text.
    /// </summary>
    public static string ActivityBarConnections => ResourceManager.GetString("ActivityBarConnections") ?? "Connections";

    /// <summary>
    /// Gets the Settings activity bar item text.
    /// </summary>
    public static string ActivityBarSettings => ResourceManager.GetString("ActivityBarSettings") ?? "Settings";

    #endregion

    #region Bottom Panel Strings

    /// <summary>
    /// Gets the Problems panel tab text.
    /// </summary>
    public static string PanelProblems => ResourceManager.GetString("PanelProblems") ?? "Problems";

    /// <summary>
    /// Gets the Output panel tab text.
    /// </summary>
    public static string PanelOutput => ResourceManager.GetString("PanelOutput") ?? "Output";

    /// <summary>
    /// Gets the Debug Console panel tab text.
    /// </summary>
    public static string PanelDebugConsole => ResourceManager.GetString("PanelDebugConsole") ?? "Debug Console";

    /// <summary>
    /// Gets the Log Viewer panel tab text.
    /// </summary>
    public static string PanelLogViewer => ResourceManager.GetString("PanelLogViewer") ?? "Log Viewer";

    #endregion

    #region Log Viewer Strings

    /// <summary>
    /// Gets the Search placeholder text.
    /// </summary>
    public static string LogViewerSearchPlaceholder => ResourceManager.GetString("LogViewerSearchPlaceholder") ?? "Search logs...";

    /// <summary>
    /// Gets the Log Level filter text.
    /// </summary>
    public static string LogViewerLogLevel => ResourceManager.GetString("LogViewerLogLevel") ?? "Log Level";

    /// <summary>
    /// Gets the Start Date filter text.
    /// </summary>
    public static string LogViewerStartDate => ResourceManager.GetString("LogViewerStartDate") ?? "Start Date";

    /// <summary>
    /// Gets the End Date filter text.
    /// </summary>
    public static string LogViewerEndDate => ResourceManager.GetString("LogViewerEndDate") ?? "End Date";

    /// <summary>
    /// Gets the Clear Logs button text.
    /// </summary>
    public static string LogViewerClearLogs => ResourceManager.GetString("LogViewerClearLogs") ?? "Clear Logs";

    /// <summary>
    /// Gets the Export Logs button text.
    /// </summary>
    public static string LogViewerExportLogs => ResourceManager.GetString("LogViewerExportLogs") ?? "Export Logs";

    /// <summary>
    /// Gets the Refresh button text.
    /// </summary>
    public static string LogViewerRefresh => ResourceManager.GetString("LogViewerRefresh") ?? "Refresh";

    #endregion

    #region Dialog Strings

    /// <summary>
    /// Gets the Confirmation dialog title.
    /// </summary>
    public static string DialogConfirmationTitle => ResourceManager.GetString("DialogConfirmationTitle") ?? "Confirmation";

    /// <summary>
    /// Gets the Error dialog title.
    /// </summary>
    public static string DialogErrorTitle => ResourceManager.GetString("DialogErrorTitle") ?? "Error";

    /// <summary>
    /// Gets the Information dialog title.
    /// </summary>
    public static string DialogInformationTitle => ResourceManager.GetString("DialogInformationTitle") ?? "Information";

    /// <summary>
    /// Gets the Warning dialog title.
    /// </summary>
    public static string DialogWarningTitle => ResourceManager.GetString("DialogWarningTitle") ?? "Warning";

    /// <summary>
    /// Gets the OK button text.
    /// </summary>
    public static string DialogOK => ResourceManager.GetString("DialogOK") ?? "OK";

    /// <summary>
    /// Gets the Cancel button text.
    /// </summary>
    public static string DialogCancel => ResourceManager.GetString("DialogCancel") ?? "Cancel";

    /// <summary>
    /// Gets the Yes button text.
    /// </summary>
    public static string DialogYes => ResourceManager.GetString("DialogYes") ?? "Yes";

    /// <summary>
    /// Gets the No button text.
    /// </summary>
    public static string DialogNo => ResourceManager.GetString("DialogNo") ?? "No";

    /// <summary>
    /// Gets the title for the exit application confirmation dialog.
    /// </summary>
    public static string Dialog_ExitTitle => ResourceManager.GetString("Dialog_ExitTitle") ?? "Exit Application";

    #endregion

    #region Status Messages

    /// <summary>
    /// Gets the Ready status message.
    /// </summary>
    public static string StatusReady => ResourceManager.GetString("StatusReady") ?? "Ready";

    /// <summary>
    /// Gets the Loading status message.
    /// </summary>
    public static string StatusLoading => ResourceManager.GetString("StatusLoading") ?? "Loading...";

    /// <summary>
    /// Gets the Saving status message.
    /// </summary>
    public static string StatusSaving => ResourceManager.GetString("StatusSaving") ?? "Saving...";

    /// <summary>
    /// Gets the Connected status message.
    /// </summary>
    public static string StatusConnected => ResourceManager.GetString("StatusConnected") ?? "Connected";

    /// <summary>
    /// Gets the Disconnected status message.
    /// </summary>
    public static string StatusDisconnected => ResourceManager.GetString("StatusDisconnected") ?? "Disconnected";

    #endregion

    #region Error Messages

    /// <summary>
    /// Gets the generic error message.
    /// </summary>
    public static string ErrorGeneric => ResourceManager.GetString("ErrorGeneric") ?? "An error occurred";

    /// <summary>
    /// Gets the connection error message.
    /// </summary>
    public static string ErrorConnection => ResourceManager.GetString("ErrorConnection") ?? "Connection error";

    /// <summary>
    /// Gets the file not found error message.
    /// </summary>
    public static string ErrorFileNotFound => ResourceManager.GetString("ErrorFileNotFound") ?? "File not found";

    /// <summary>
    /// Gets the access denied error message.
    /// </summary>
    public static string ErrorAccessDenied => ResourceManager.GetString("ErrorAccessDenied") ?? "Access denied";

    /// <summary>
    /// Gets the invalid operation error message.
    /// </summary>
    public static string ErrorInvalidOperation => ResourceManager.GetString("ErrorInvalidOperation") ?? "Invalid operation";

    #endregion

    #region Confirmation Messages

    /// <summary>
    /// Gets the exit application confirmation message.
    /// </summary>
    public static string Confirm_Exit => ResourceManager.GetString("Confirm_Exit") ?? "Are you sure you want to exit?";

    /// <summary>
    /// Gets the clear logs confirmation message.
    /// </summary>
    public static string Confirm_ClearLogs => ResourceManager.GetString("Confirm_ClearLogs") ?? "Are you sure you want to clear all logs?";

    #endregion

    #region Validation Messages

    /// <summary>
    /// Gets the required field validation message.
    /// </summary>
    public static string ValidationRequired => ResourceManager.GetString("ValidationRequired") ?? "This field is required";

    /// <summary>
    /// Gets the invalid format validation message.
    /// </summary>
    public static string ValidationInvalidFormat => ResourceManager.GetString("ValidationInvalidFormat") ?? "Invalid format";

    /// <summary>
    /// Gets the value out of range validation message.
    /// </summary>
    public static string ValidationOutOfRange => ResourceManager.GetString("ValidationOutOfRange") ?? "Value is out of range";

    #endregion

    #region Log Viewer Strings

    /// <summary>
    /// Gets the title for the clear logs confirmation dialog.
    /// </summary>
    public static string LogViewer_ClearLogsTitle => GetStringSafe("LogViewer_ClearLogsTitle", "Clear Logs");

    /// <summary>
    /// Gets the title for the export logs dialog.
    /// </summary>
    public static string LogViewer_ExportLogsTitle => GetStringSafe("LogViewer_ExportLogsTitle", "Export Logs");

    /// <summary>
    /// Gets the message for the clear logs confirmation dialog.
    /// </summary>
    public static string LogViewer_ClearLogsMessage =>
        GetStringSafe("LogViewer_ClearLogsMessage", "Are you sure you want to clear all log entries? This action cannot be undone.");

    /// <summary>
    /// Gets the error message when export service is unavailable.
    /// </summary>
    public static string LogViewer_ExportServiceUnavailable =>
        GetStringSafe("LogViewer_ExportServiceUnavailable", "Export service is not available");

    /// <summary>
    /// Gets the message when there are no logs to export.
    /// </summary>
    public static string LogViewer_NoLogsToExport =>
        GetStringSafe("LogViewer_NoLogsToExport", "No log entries to export. Check your filters.");

    /// <summary>
    /// Gets the success message template after exporting logs.
    /// Expects parameters: count (int), format (string)
    /// </summary>
    public static string LogViewer_ExportSuccess =>
        GetStringSafe("LogViewer_ExportSuccess", "Successfully exported {0} log entries to {1} format.");

    /// <summary>
    /// Gets the title for export failed error dialog.
    /// </summary>
    public static string LogViewer_ExportFailed =>
        GetStringSafe("LogViewer_ExportFailed", "Export Failed");

    /// <summary>
    /// Gets the error message template when export fails.
    /// Expects parameter: error message (string)
    /// </summary>
    public static string LogViewer_ExportFailedMessage =>
        GetStringSafe("LogViewer_ExportFailedMessage", "Failed to export logs: {0}");

    /// <summary>
    /// Gets the generic unknown error message.
    /// </summary>
    public static string LogViewer_UnknownError =>
        GetStringSafe("LogViewer_UnknownError", "Unknown error occurred");

    #endregion

    #region Clipboard Operations

    /// <summary>
    /// Gets the "Text cut to clipboard" status message.
    /// </summary>
    public static string ClipboardTextCut => GetStringSafe("ClipboardTextCut", "Text cut to clipboard");

    /// <summary>
    /// Gets the "Text copied to clipboard" status message.
    /// </summary>
    public static string ClipboardTextCopied => GetStringSafe("ClipboardTextCopied", "Text copied to clipboard");

    /// <summary>
    /// Gets the "Text pasted from clipboard" status message.
    /// </summary>
    public static string ClipboardTextPasted => GetStringSafe("ClipboardTextPasted", "Text pasted from clipboard");

    /// <summary>
    /// Gets the "Failed to cut text" error message.
    /// </summary>
    public static string ClipboardCutFailed => GetStringSafe("ClipboardCutFailed", "Failed to cut text");

    /// <summary>
    /// Gets the "Failed to copy text" error message.
    /// </summary>
    public static string ClipboardCopyFailed => GetStringSafe("ClipboardCopyFailed", "Failed to copy text");

    /// <summary>
    /// Gets the "Failed to paste text" error message.
    /// </summary>
    public static string ClipboardPasteFailed => GetStringSafe("ClipboardPasteFailed", "Failed to paste text");

    #endregion

    #region Log Testing

    /// <summary>
    /// Gets the formatted "Generated {0} log message" status message.
    /// </summary>
    /// <param name="levelName">The log level name.</param>
    /// <returns>The formatted message.</returns>
    public static string LogTestGenerated(string levelName) =>
        GetStringSafe("LogTestGenerated", "Generated {0} log message").Replace("{0}", levelName);

    /// <summary>
    /// Gets the formatted "Failed to generate {0} log" error message.
    /// </summary>
    /// <param name="levelName">The log level name.</param>
    /// <returns>The formatted message.</returns>
    public static string LogTestFailed(string levelName) =>
        GetStringSafe("LogTestFailed", "Failed to generate {0} log").Replace("{0}", levelName);

    /// <summary>
    /// Gets the "Log export copied to clipboard" status message.
    /// </summary>
    public static string LogExportCopied => GetStringSafe("LogExportCopied", "Log export copied to clipboard");

    /// <summary>
    /// Gets the test clipboard text.
    /// </summary>
    public static string TestClipboardText => GetStringSafe("TestClipboardText", "This is some text to test clipboard operations.");

    #endregion

    #region Settings Service Errors

    /// <summary>
    /// Gets the error message for settings not loaded.
    /// </summary>
    public static string Error_SettingsNotLoaded => GetStringSafe("Error_SettingsNotLoaded", "Settings not loaded. Call LoadSettingsAsync first.");

    /// <summary>
    /// Gets the error message for settings save failure.
    /// </summary>
    public static string Error_SettingsSaveFailed => GetStringSafe("Error_SettingsSaveFailed", "Failed to save user settings");

    /// <summary>
    /// Gets the error message format for setting update failure.
    /// </summary>
    public static string Error_SettingUpdateFailed => GetStringSafe("Error_SettingUpdateFailed", "Failed to set setting: {0}");

    /// <summary>
    /// Gets the error message format for setting reset failure.
    /// </summary>
    public static string Error_SettingResetFailed => GetStringSafe("Error_SettingResetFailed", "Failed to reset setting: {0}");

    /// <summary>
    /// Gets the error message for reset all settings failure.
    /// </summary>
    public static string Error_SettingsResetAllFailed => GetStringSafe("Error_SettingsResetAllFailed", "Failed to reset all settings");

    /// <summary>
    /// Gets the error message for restore defaults failure.
    /// </summary>
    public static string Error_SettingsRestoreDefaultsFailed => GetStringSafe("Error_SettingsRestoreDefaultsFailed", "Failed to restore default settings");

    /// <summary>
    /// Gets the error message for invalid JSON.
    /// </summary>
    public static string Error_SettingsInvalidJson => GetStringSafe("Error_SettingsInvalidJson", "Invalid JSON in settings file");

    /// <summary>
    /// Gets the error message for file load failure.
    /// </summary>
    public static string Error_SettingsFileLoadFailed => GetStringSafe("Error_SettingsFileLoadFailed", "Failed to load user settings file");

    /// <summary>
    /// Gets the error message for file save failure.
    /// </summary>
    public static string Error_SettingsFileSaveFailed => GetStringSafe("Error_SettingsFileSaveFailed", "Failed to save user settings file");

    /// <summary>
    /// Gets the error message for null or empty setting key.
    /// </summary>
    public static string Error_SettingKeyNullOrEmpty => GetStringSafe("Error_SettingKeyNullOrEmpty", "Setting key cannot be null or empty");

    #endregion

    #region Profile Management Errors

    /// <summary>
    /// Gets the error message for empty profile name.
    /// </summary>
    public static string Error_ProfileNameEmpty => GetStringSafe("Error_ProfileNameEmpty", "Profile name cannot be empty.");

    /// <summary>
    /// Gets the error message for unique name generation failure.
    /// </summary>
    public static string Error_UniqueNameGenerationFailed => GetStringSafe("Error_UniqueNameGenerationFailed", "Unable to generate unique name after 1000 attempts.");

    #endregion

    #region Common Status Messages

    /// <summary>
    /// Gets the status message when a profile is loaded.
    /// </summary>
    public static string Status_ProfileLoaded => GetStringSafe("Status_ProfileLoaded", "Profile loaded");

    /// <summary>
    /// Gets the status message when saving a profile.
    /// </summary>
    public static string Status_SavingProfile => GetStringSafe("Status_SavingProfile", "Saving profile...");

    /// <summary>
    /// Gets the status message when a profile is saved successfully.
    /// </summary>
    public static string Status_ProfileSaved => GetStringSafe("Status_ProfileSaved", "Profile saved successfully");

    /// <summary>
    /// Gets the status message format when save fails.
    /// Expects parameter: error message (string)
    /// </summary>
    public static string Status_SaveFailed => GetStringSafe("Status_SaveFailed", "Save failed: {0}");

    /// <summary>
    /// Gets the status message format when save fails due to invalid profile data.
    /// Expects parameter: error message (string)
    /// </summary>
    public static string Status_InvalidProfileData => GetStringSafe("Status_InvalidProfileData", "Save failed: Invalid profile data - {0}");

    /// <summary>
    /// Gets the status message when changes are cancelled.
    /// </summary>
    public static string Status_ChangesCancelled => GetStringSafe("Status_ChangesCancelled", "Changes cancelled");

    /// <summary>
    /// Gets the generic error status message format.
    /// Expects parameters: operation (string), error message (string)
    /// </summary>
    public static string Status_Error => GetStringSafe("Status_Error", "{0}: {1}");

    /// <summary>
    /// Gets the status message format when socat process starts.
    /// Expects parameters: process ID (int), TCP port (int)
    /// </summary>
    public static string Status_SocatProcessStarted => GetStringSafe("Status_SocatProcessStarted", "socat process {0} started on port {1}");

    /// <summary>
    /// Gets the status message format when socat process stops.
    /// Expects parameter: process ID (int)
    /// </summary>
    public static string Status_SocatProcessStopped => GetStringSafe("Status_SocatProcessStopped", "socat process {0} stopped");

    /// <summary>
    /// Gets the status message format when socat process encounters an error.
    /// Expects parameters: process ID (int), error message (string)
    /// </summary>
    public static string Status_SocatProcessError => GetStringSafe("Status_SocatProcessError", "socat process {0} error: {1}");

    /// <summary>
    /// Gets the status message format when connection is established.
    /// Expects parameter: process ID (int)
    /// </summary>
    public static string Status_SocatConnectionEstablished => GetStringSafe("Status_SocatConnectionEstablished", "Connection established to process {0}");

    /// <summary>
    /// Gets the status message format when connection is closed.
    /// Expects parameter: process ID (int)
    /// </summary>
    public static string Status_SocatConnectionClosed => GetStringSafe("Status_SocatConnectionClosed", "Connection closed to process {0}");

    /// <summary>
    /// Gets the status message when scanning for devices.
    /// </summary>
    public static string Status_ScanningDevices => GetStringSafe("Status_ScanningDevices", "Scanning for serial devices...");

    /// <summary>
    /// Gets the status message format when devices are found.
    /// Expects parameter: device count (int)
    /// </summary>
    public static string Status_DevicesFound => GetStringSafe("Status_DevicesFound", "Found {0} serial device(s)");

    /// <summary>
    /// Gets the status message when device scanning fails.
    /// </summary>
    public static string Status_ErrorScanningDevices => GetStringSafe("Status_ErrorScanningDevices", "Error scanning devices");

    /// <summary>
    /// Gets the status message when refreshing processes.
    /// </summary>
    public static string Status_RefreshingProcesses => GetStringSafe("Status_RefreshingProcesses", "Refreshing running processes...");

    /// <summary>
    /// Gets the status message format when processes are found.
    /// Expects parameter: process count (int)
    /// </summary>
    public static string Status_ProcessesFound => GetStringSafe("Status_ProcessesFound", "Found {0} running process(es)");

    /// <summary>
    /// Gets the status message format when process refresh fails.
    /// Expects parameter: error message (string)
    /// </summary>
    public static string Status_ErrorRefreshingProcesses => GetStringSafe("Status_ErrorRefreshingProcesses", "Error refreshing processes: {0}");

    /// <summary>
    /// Gets the status message when deleting a profile.
    /// </summary>
    public static string Status_DeletingProfile => GetStringSafe("Status_DeletingProfile", "Deleting profile...");

    /// <summary>
    /// Gets the status message format when profile is deleted.
    /// Expects parameter: profile name (string)
    /// </summary>
    public static string Status_ProfileDeleted => GetStringSafe("Status_ProfileDeleted", "Profile '{0}' deleted successfully");

    /// <summary>
    /// Gets the status message when profile deletion fails.
    /// </summary>
    public static string Status_DeleteProfileFailed => GetStringSafe("Status_DeleteProfileFailed", "Failed to delete profile");

    /// <summary>
    /// Gets the status message when profile deletion encounters an error.
    /// </summary>
    public static string Status_ErrorDeletingProfile => GetStringSafe("Status_ErrorDeletingProfile", "Error deleting profile");

    /// <summary>
    /// Gets the status message when duplicating a profile.
    /// </summary>
    public static string Status_DuplicatingProfile => GetStringSafe("Status_DuplicatingProfile", "Duplicating profile...");

    /// <summary>
    /// Gets the status message format when profile is duplicated.
    /// Expects parameter: new profile name (string)
    /// </summary>
    public static string Status_ProfileDuplicated => GetStringSafe("Status_ProfileDuplicated", "Profile duplicated as '{0}'");

    /// <summary>
    /// Gets the status message when profile duplication fails.
    /// </summary>
    public static string Status_ErrorDuplicatingProfile => GetStringSafe("Status_ErrorDuplicatingProfile", "Error duplicating profile");

    /// <summary>
    /// Gets the status message format when starting socat.
    /// Expects parameter: TCP port (int)
    /// </summary>
    public static string Status_StartingSocat => GetStringSafe("Status_StartingSocat", "Starting socat on port {0}...");

    /// <summary>
    /// Gets the status message format when socat starts successfully.
    /// Expects parameter: process ID (int)
    /// </summary>
    public static string Status_SocatStarted => GetStringSafe("Status_SocatStarted", "socat started successfully (PID: {0})");

    /// <summary>
    /// Gets the status message format when starting socat fails.
    /// Expects parameter: error message (string)
    /// </summary>
    public static string Status_ErrorStartingSocat => GetStringSafe("Status_ErrorStartingSocat", "Error starting socat: {0}");

    /// <summary>
    /// Gets the status message format when stopping socat.
    /// Expects parameter: process ID (int)
    /// </summary>
    public static string Status_StoppingSocat => GetStringSafe("Status_StoppingSocat", "Stopping socat process {0}...");

    /// <summary>
    /// Gets the status message format when socat stops successfully.
    /// Expects parameter: process ID (int)
    /// </summary>
    public static string Status_SocatStoppedSuccessfully => GetStringSafe("Status_SocatStoppedSuccessfully", "socat process {0} stopped successfully");

    /// <summary>
    /// Gets the status message format when stopping socat fails.
    /// Expects parameter: process ID (int)
    /// </summary>
    public static string Status_SocatStopFailed => GetStringSafe("Status_SocatStopFailed", "Failed to stop socat process {0}");

    /// <summary>
    /// Gets the status message when stopping all socat processes.
    /// </summary>
    public static string Status_StoppingAllSocat => GetStringSafe("Status_StoppingAllSocat", "Stopping all socat processes...");

    /// <summary>
    /// Gets the status message format when all socat processes stopped.
    /// Expects parameter: process count (int)
    /// </summary>
    public static string Status_AllSocatStopped => GetStringSafe("Status_AllSocatStopped", "Stopped {0} socat process(es)");

    /// <summary>
    /// Gets the status message when stopping all socat processes fails.
    /// </summary>
    public static string Status_ErrorStoppingAllSocat => GetStringSafe("Status_ErrorStoppingAllSocat", "Error stopping all socat processes");

    /// <summary>
    /// Gets the status message format when testing connection.
    /// Expects parameter: port number (int)
    /// </summary>
    public static string Status_TestingConnection => GetStringSafe("Status_TestingConnection", "Testing connection to port {0}...");

    /// <summary>
    /// Gets the status message format when connection succeeds.
    /// Expects parameter: port number (int)
    /// </summary>
    public static string Status_ConnectionSuccess => GetStringSafe("Status_ConnectionSuccess", "Connection to port {0} successful");

    /// <summary>
    /// Gets the status message format when connection fails.
    /// Expects parameter: port number (int)
    /// </summary>
    public static string Status_ConnectionFailed => GetStringSafe("Status_ConnectionFailed", "Connection to port {0} failed");

    /// <summary>
    /// Gets the status message when connection test encounters an error.
    /// </summary>
    public static string Status_ErrorTestingConnection => GetStringSafe("Status_ErrorTestingConnection", "Error testing connection");

    /// <summary>
    /// Gets the status message when file dialog service is unavailable.
    /// </summary>
    public static string Status_FileDialogUnavailable => GetStringSafe("Status_FileDialogUnavailable", "File dialog service not available");

    /// <summary>
    /// Gets the status message when exporting profiles.
    /// </summary>
    public static string Status_ExportingProfiles => GetStringSafe("Status_ExportingProfiles", "Exporting profiles...");

    /// <summary>
    /// Gets the status message format when profiles are exported.
    /// Expects parameters: count (int), filename (string)
    /// </summary>
    public static string Status_ProfilesExported => GetStringSafe("Status_ProfilesExported", "Exported {0} profile(s) to {1}");

    /// <summary>
    /// Gets the status message when profile export fails.
    /// </summary>
    public static string Status_ErrorExportingProfiles => GetStringSafe("Status_ErrorExportingProfiles", "Error exporting profiles");

    /// <summary>
    /// Gets the status message when importing profiles.
    /// </summary>
    public static string Status_ImportingProfiles => GetStringSafe("Status_ImportingProfiles", "Importing profiles...");

    /// <summary>
    /// Gets the status message format when profiles are imported.
    /// Expects parameters: count (int), filename (string)
    /// </summary>
    public static string Status_ProfilesImported => GetStringSafe("Status_ProfilesImported", "Imported {0} profile(s) from {1}");

    /// <summary>
    /// Gets the status message when profile import fails.
    /// </summary>
    public static string Status_ErrorImportingProfiles => GetStringSafe("Status_ErrorImportingProfiles", "Error importing profiles");

    /// <summary>
    /// Gets the status message when exporting selected profile.
    /// </summary>
    public static string Status_ExportingSelectedProfile => GetStringSafe("Status_ExportingSelectedProfile", "Exporting selected profile...");

    /// <summary>
    /// Gets the status message when profile exported successfully.
    /// </summary>
    public static string Status_ProfileExportedSuccessfully => GetStringSafe("Status_ProfileExportedSuccessfully", "Profile exported successfully");

    /// <summary>
    /// Gets the status message when exporting profile fails.
    /// </summary>
    public static string Status_ErrorExportingProfile => GetStringSafe("Status_ErrorExportingProfile", "Error exporting profile");

    /// <summary>
    /// Gets the status message when showing profile details fails.
    /// </summary>
    public static string Status_ErrorShowingProfileDetails => GetStringSafe("Status_ErrorShowingProfileDetails", "Error showing profile details");

    /// <summary>
    /// Gets the status message when directory selection fails.
    /// </summary>
    public static string Status_ErrorSelectingDirectory => GetStringSafe("Status_ErrorSelectingDirectory", "Error selecting directory");

    /// <summary>
    /// Gets the status message when opening profiles folder.
    /// </summary>
    public static string Status_OpeningProfilesFolder => GetStringSafe("Status_OpeningProfilesFolder", "Opening profiles folder...");

    /// <summary>
    /// Gets the status message when profiles path is not available.
    /// </summary>
    public static string Status_ProfilesPathNotAvailable => GetStringSafe("Status_ProfilesPathNotAvailable", "Profiles path not available");

    /// <summary>
    /// Gets the status message when creating profiles folder.
    /// </summary>
    public static string Status_CreatingProfilesFolder => GetStringSafe("Status_CreatingProfilesFolder", "Creating profiles folder...");

    /// <summary>
    /// Gets the status message when profiles folder opened successfully.
    /// </summary>
    public static string Status_ProfilesFolderOpened => GetStringSafe("Status_ProfilesFolderOpened", "Profiles folder opened");

    /// <summary>
    /// Gets the status message when opening profiles folder fails.
    /// </summary>
    public static string Status_ErrorOpeningProfilesFolder => GetStringSafe("Status_ErrorOpeningProfilesFolder", "Error opening profiles folder");

    /// <summary>
    /// Gets the status message when profiles path reset to default.
    /// </summary>
    public static string Status_ProfilesPathReset => GetStringSafe("Status_ProfilesPathReset", "Profiles path reset to default");

    /// <summary>
    /// Gets the status message when resetting profiles path fails.
    /// </summary>
    public static string Status_ErrorResettingProfilesPath => GetStringSafe("Status_ErrorResettingProfilesPath", "Error resetting profiles path");

    /// <summary>
    /// Gets the status message when profiles path updated successfully.
    /// </summary>
    public static string Status_ProfilesPathUpdated => GetStringSafe("Status_ProfilesPathUpdated", "Profiles path updated");

    /// <summary>
    /// Gets the status message when settings update fails.
    /// </summary>
    public static string Status_FailedToUpdateSettings => GetStringSafe("Status_FailedToUpdateSettings", "Failed to update settings");

    #endregion

    #region Validation Messages - PowerSupply

    /// <summary>
    /// Gets the validation message when profile name is required.
    /// </summary>
    public static string Validation_ProfileNameRequired => GetStringSafe("Validation_ProfileNameRequired", "Profile name is required");

    /// <summary>
    /// Gets the validation message when profile name is too long.
    /// </summary>
    public static string Validation_ProfileNameTooLong => GetStringSafe("Validation_ProfileNameTooLong", "Profile name is too long (max 255 characters)");

    /// <summary>
    /// Gets the validation message when profile description is too long.
    /// </summary>
    public static string Validation_ProfileDescriptionTooLong => GetStringSafe("Validation_ProfileDescriptionTooLong", "Profile description is too long (max 1000 characters)");

    /// <summary>
    /// Gets the validation message when ModbusTcp host is required.
    /// </summary>
    public static string Validation_ModbusTcpHostRequired => GetStringSafe("Validation_ModbusTcpHostRequired", "ModbusTcp host/IP address is required");

    /// <summary>
    /// Gets the validation message when ModbusTcp port is out of range.
    /// </summary>
    public static string Validation_ModbusTcpPortRange => GetStringSafe("Validation_ModbusTcpPortRange", "ModbusTcp port must be between 1 and 65535");

    /// <summary>
    /// Gets the validation message when ModbusTcp device ID is out of range.
    /// </summary>
    public static string Validation_ModbusTcpDeviceIdRange => GetStringSafe("Validation_ModbusTcpDeviceIdRange", "ModbusTcp device ID must be between 0 and 255");

    /// <summary>
    /// Gets the validation message when ModbusTcp coil address is out of range.
    /// </summary>
    public static string Validation_ModbusTcpCoilRange => GetStringSafe("Validation_ModbusTcpCoilRange", "ModbusTcp coil address must be between 0 and 65535");

    /// <summary>
    /// Gets the ready status message.
    /// </summary>
    public static string Status_Ready => GetStringSafe("Status_Ready", "Ready");

    #endregion

    #region Jobs Management Messages

    /// <summary>
    /// Gets the status message when no job is selected for editing.
    /// </summary>
    public static string Status_NoJobSelectedToEdit => GetStringSafe("Status_NoJobSelectedToEdit", "No job selected to edit");

    /// <summary>
    /// Gets the status message when creating a job from a template.
    /// </summary>
    public static string Status_CreatingJobFromTemplate => GetStringSafe("Status_CreatingJobFromTemplate", "Creating job from template...");

    /// <summary>
    /// Gets the status message when no job templates are available.
    /// </summary>
    public static string Status_NoTemplatesAvailable => GetStringSafe("Status_NoTemplatesAvailable", "No templates available");

    /// <summary>
    /// Gets the status message format when job is created from template.
    /// Expects parameters: new job name, template name
    /// </summary>
    public static string Status_CreatedJobFromTemplate => GetStringSafe("Status_CreatedJobFromTemplate", "Created job '{0}' from template '{1}'");

    /// <summary>
    /// Gets the error message format when creating job from template fails.
    /// Expects parameter: error message
    /// </summary>
    public static string Status_ErrorCreatingJobFromTemplate => GetStringSafe("Status_ErrorCreatingJobFromTemplate", "Error creating job from template: {0}");

    /// <summary>
    /// Gets the status message when saving a job as a template.
    /// </summary>
    public static string Status_SavingJobAsTemplate => GetStringSafe("Status_SavingJobAsTemplate", "Saving job as template...");

    /// <summary>
    /// Gets the status message format when job is saved as template.
    /// Expects parameter: job name
    /// </summary>
    public static string Status_JobSavedAsTemplate => GetStringSafe("Status_JobSavedAsTemplate", "Job '{0}' saved as template");

    /// <summary>
    /// Gets the status message format when saving job as template fails.
    /// Expects parameter: job name
    /// </summary>
    public static string Status_FailedToSaveJobAsTemplate => GetStringSafe("Status_FailedToSaveJobAsTemplate", "Failed to save job '{0}' as template");

    /// <summary>
    /// Gets the error message format when saving job as template fails.
    /// Expects parameter: error message
    /// </summary>
    public static string Status_ErrorSavingJobAsTemplate => GetStringSafe("Status_ErrorSavingJobAsTemplate", "Error saving job as template: {0}");

    /// <summary>
    /// Gets the status message when importing jobs.
    /// </summary>
    public static string Status_ImportingJobs => GetStringSafe("Status_ImportingJobs", "Importing jobs...");

    /// <summary>
    /// Gets the status message for unimplemented job import feature.
    /// </summary>
    public static string Status_JobImportNotImplemented => GetStringSafe("Status_JobImportNotImplemented", "Job import functionality not yet implemented");

    /// <summary>
    /// Gets the error message format when importing jobs fails.
    /// Expects parameter: error message
    /// </summary>
    public static string Status_ErrorImportingJobs => GetStringSafe("Status_ErrorImportingJobs", "Error importing jobs: {0}");

    /// <summary>
    /// Gets the status message when exporting a job.
    /// </summary>
    public static string Status_ExportingJob => GetStringSafe("Status_ExportingJob", "Exporting job...");

    /// <summary>
    /// Gets the status message format for unimplemented job export feature.
    /// Expects parameter: job name
    /// </summary>
    public static string Status_ExportJobNotImplemented => GetStringSafe("Status_ExportJobNotImplemented", "Export functionality for job '{0}' not yet implemented");

    /// <summary>
    /// Gets the error message format when exporting job fails.
    /// Expects parameter: error message
    /// </summary>
    public static string Status_ErrorExportingJob => GetStringSafe("Status_ErrorExportingJob", "Error exporting job: {0}");

    /// <summary>
    /// Gets the status message when creating a task from a job.
    /// </summary>
    public static string Status_CreatingTaskFromJob => GetStringSafe("Status_CreatingTaskFromJob", "Creating task from job...");

    /// <summary>
    /// Gets the status message format for unimplemented task creation from job feature.
    /// Expects parameter: job name
    /// </summary>
    public static string Status_TaskCreationFromJobNotImplemented => GetStringSafe("Status_TaskCreationFromJobNotImplemented", "Task creation from job '{0}' not yet implemented");

    /// <summary>
    /// Gets the error message format when creating task from job fails.
    /// Expects parameter: error message
    /// </summary>
    public static string Status_ErrorCreatingTaskFromJob => GetStringSafe("Status_ErrorCreatingTaskFromJob", "Error creating task from job: {0}");

    /// <summary>
    /// Gets the status message when validating a job.
    /// </summary>
    public static string Status_ValidatingJob => GetStringSafe("Status_ValidatingJob", "Validating job...");

    /// <summary>
    /// Gets the status message format when job validation passes.
    /// Expects parameter: job name
    /// </summary>
    public static string Status_JobIsValid => GetStringSafe("Status_JobIsValid", "Job '{0}' is valid");

    /// <summary>
    /// Gets the status message format when job validation fails.
    /// Expects parameters: job name, error summary
    /// </summary>
    public static string Status_JobHasValidationErrors => GetStringSafe("Status_JobHasValidationErrors", "Job '{0}' has validation errors: {1}");

    /// <summary>
    /// Gets the error message format when job validation fails.
    /// Expects parameter: error message
    /// </summary>
    public static string Status_ErrorValidatingJob => GetStringSafe("Status_ErrorValidatingJob", "Error validating job: {0}");

    /// <summary>
    /// Gets the status message when opening the job creation wizard.
    /// </summary>
    public static string Status_OpeningJobCreationWizard => GetStringSafe("Status_OpeningJobCreationWizard", "Opening job creation wizard...");

    /// <summary>
    /// Gets the error message when opening wizard fails.
    /// </summary>
    public static string Status_ErrorOpeningWizard => GetStringSafe("Status_ErrorOpeningWizard", "Error opening wizard");

    #endregion

    #region Logging Settings Messages

    /// <summary>
    /// Gets the status message when saving settings.
    /// </summary>
    public static string Status_SavingSettings => GetStringSafe("Status_SavingSettings", "Saving settings...");

    /// <summary>
    /// Gets the status message when settings are saved successfully.
    /// </summary>
    public static string Status_SettingsSavedSuccessfully => GetStringSafe("Status_SettingsSavedSuccessfully", "Settings saved successfully");

    /// <summary>
    /// Gets the error message when saving settings fails.
    /// </summary>
    public static string Status_ErrorSavingSettings => GetStringSafe("Status_ErrorSavingSettings", "Error saving settings");

    /// <summary>
    /// Gets the status message when loading settings.
    /// </summary>
    public static string Status_LoadingSettings => GetStringSafe("Status_LoadingSettings", "Loading settings...");

    /// <summary>
    /// Gets the status message when settings are loaded successfully.
    /// </summary>
    public static string Status_SettingsLoadedSuccessfully => GetStringSafe("Status_SettingsLoadedSuccessfully", "Settings loaded successfully");

    /// <summary>
    /// Gets the error message when loading settings fails.
    /// </summary>
    public static string Status_ErrorLoadingSettings => GetStringSafe("Status_ErrorLoadingSettings", "Error loading settings");

    /// <summary>
    /// Gets the status message when resetting settings to defaults.
    /// </summary>
    public static string Status_ResettingToDefaults => GetStringSafe("Status_ResettingToDefaults", "Resetting to defaults...");

    /// <summary>
    /// Gets the status message when settings are reset to defaults successfully.
    /// </summary>
    public static string Status_SettingsResetToDefaults => GetStringSafe("Status_SettingsResetToDefaults", "Settings reset to defaults successfully");

    /// <summary>
    /// Gets the error message when resetting settings fails.
    /// </summary>
    public static string Status_ErrorResettingSettings => GetStringSafe("Status_ErrorResettingSettings", "Error resetting settings");

    /// <summary>
    /// Gets the error message when opening settings directory fails.
    /// </summary>
    public static string Status_ErrorOpeningSettingsDirectory => GetStringSafe("Status_ErrorOpeningSettingsDirectory", "Error opening settings directory");

    /// <summary>
    /// Gets the status message when default log path is not configured.
    /// </summary>
    public static string Status_DefaultLogPathNotSet => GetStringSafe("Status_DefaultLogPathNotSet", "Default log path is not set");

    /// <summary>
    /// Gets the error message when opening default log path fails.
    /// </summary>
    public static string Status_ErrorOpeningDefaultLogPath => GetStringSafe("Status_ErrorOpeningDefaultLogPath", "Error opening default log path");

    /// <summary>
    /// Gets the status message when export path is not configured.
    /// </summary>
    public static string Status_ExportPathNotSet => GetStringSafe("Status_ExportPathNotSet", "Export path is not set");

    /// <summary>
    /// Gets the error message when opening export path fails.
    /// </summary>
    public static string Status_ErrorOpeningExportPath => GetStringSafe("Status_ErrorOpeningExportPath", "Error opening export path");

    #endregion

    #region Error Messages - Operations

    /// <summary>
    /// Gets the error message format for invalid operations.
    /// Expects parameter: operation name (string)
    /// </summary>
    public static string Error_InvalidOperation => GetStringSafe("Error_InvalidOperation", "Invalid operation while {0}");

    /// <summary>
    /// Gets the error message format for invalid data.
    /// Expects parameter: operation name (string)
    /// </summary>
    public static string Error_InvalidData => GetStringSafe("Error_InvalidData", "Invalid data while {0}");

    /// <summary>
    /// Gets the error message format for access denied.
    /// Expects parameter: operation name (string)
    /// </summary>
    public static string Error_AccessDeniedOperation => GetStringSafe("Error_AccessDeniedOperation", "Access denied while {0}");

    /// <summary>
    /// Gets the generic error message format.
    /// Expects parameter: operation name (string)
    /// </summary>
    public static string Error_Generic => GetStringSafe("Error_Generic", "Error {0}");

    #endregion

    /// <summary>
    /// Gets a formatted string with the specified arguments.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="args">The formatting arguments.</param>
    /// <returns>The formatted string.</returns>
    public static string GetFormatted(string key, params object[] args)
    {
        return ResourceManager.GetString(key, args);
    }

    /// <summary>
    /// Gets a string for the specified culture.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="culture">The culture to use.</param>
    /// <returns>The localized string.</returns>
    public static string GetString(string key, CultureInfo culture)
    {
        return ResourceManager.GetString(key, culture);
    }

    /// <summary>
    /// Safely gets a resource string with fallback if ResourceManager is not initialized.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <param name="fallback">The fallback value to use if ResourceManager is not available.</param>
    /// <returns>The localized string or the fallback value.</returns>
    private static string GetStringSafe(string key, string fallback)
    {
        try
        {
            return IsInitialized ? (ResourceManager.GetString(key) ?? fallback) : fallback;
        }
        catch
        {
            // Return fallback if any exception occurs
            return fallback;
        }
    }
}
