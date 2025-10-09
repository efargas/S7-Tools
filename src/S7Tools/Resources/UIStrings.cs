using S7Tools.Core.Resources;
using System.Globalization;

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
        get => _resourceManager ?? throw new InvalidOperationException("ResourceManager not initialized");
        set => _resourceManager = value ?? throw new ArgumentNullException(nameof(value));
    }

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
    public static string LogViewer_ClearLogsTitle => ResourceManager.GetString("LogViewer_ClearLogsTitle") ?? "Clear Logs";

    /// <summary>
    /// Gets the title for the export logs dialog.
    /// </summary>
    public static string LogViewer_ExportLogsTitle => ResourceManager.GetString("LogViewer_ExportLogsTitle") ?? "Export Logs";

    /// <summary>
    /// Gets the message for the clear logs confirmation dialog.
    /// </summary>
    public static string LogViewer_ClearLogsMessage => 
        ResourceManager.GetString("LogViewer_ClearLogsMessage") ?? 
        "Are you sure you want to clear all log entries? This action cannot be undone.";

    /// <summary>
    /// Gets the error message when export service is unavailable.
    /// </summary>
    public static string LogViewer_ExportServiceUnavailable => 
        ResourceManager.GetString("LogViewer_ExportServiceUnavailable") ?? "Export service is not available";

    /// <summary>
    /// Gets the message when there are no logs to export.
    /// </summary>
    public static string LogViewer_NoLogsToExport => 
        ResourceManager.GetString("LogViewer_NoLogsToExport") ?? "No log entries to export. Check your filters.";

    /// <summary>
    /// Gets the success message template after exporting logs.
    /// Expects parameters: count (int), format (string)
    /// </summary>
    public static string LogViewer_ExportSuccess => 
        ResourceManager.GetString("LogViewer_ExportSuccess") ?? "Successfully exported {0} log entries to {1} format.";

    /// <summary>
    /// Gets the title for export failed error dialog.
    /// </summary>
    public static string LogViewer_ExportFailed => 
        ResourceManager.GetString("LogViewer_ExportFailed") ?? "Export Failed";

    /// <summary>
    /// Gets the error message template when export fails.
    /// Expects parameter: error message (string)
    /// </summary>
    public static string LogViewer_ExportFailedMessage => 
        ResourceManager.GetString("LogViewer_ExportFailedMessage") ?? "Failed to export logs: {0}";

    /// <summary>
    /// Gets the generic unknown error message.
    /// </summary>
    public static string LogViewer_UnknownError => 
        ResourceManager.GetString("LogViewer_UnknownError") ?? "Unknown error occurred";

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
}