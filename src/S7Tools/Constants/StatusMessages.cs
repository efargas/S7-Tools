namespace S7Tools.Constants;

/// <summary>
/// Provides constant strings for common status messages used throughout the application.
/// Centralizing these strings improves maintainability and consistency.
/// </summary>
public static class StatusMessages
{
    #region Generic Messages
    
    /// <summary>
    /// Message displayed when a file dialog service is not available.
    /// </summary>
    public const string FileDialogNotAvailable = "File dialog service not available";
    
    /// <summary>
    /// Message displayed when an operation is cancelled.
    /// </summary>
    public const string OperationCancelled = "Changes cancelled";
    
    /// <summary>
    /// Message displayed when settings have been reset to defaults.
    /// </summary>
    public const string ResetToDefaults = "Reset to default configuration";
    
    /// <summary>
    /// Message displayed when validation fails.
    /// </summary>
    public const string ValidationError = "Validation error";
    
    /// <summary>
    /// Message displayed when settings update fails.
    /// </summary>
    public const string FailedToUpdateSettings = "Failed to update settings";
    
    #endregion

    #region Profile Management Messages
    
    /// <summary>
    /// Message displayed when loading profiles.
    /// </summary>
    public const string LoadingProfiles = "Loading profiles...";
    
    /// <summary>
    /// Message displayed when saving a profile.
    /// </summary>
    public const string SavingProfile = "Saving profile...";
    
    /// <summary>
    /// Message displayed when a profile is saved successfully.
    /// </summary>
    public const string ProfileSavedSuccessfully = "Profile saved successfully";
    
    /// <summary>
    /// Message displayed when setting a default profile.
    /// </summary>
    public const string SettingDefaultProfile = "Setting default profile...";
    
    /// <summary>
    /// Message displayed when exporting profiles.
    /// </summary>
    public const string ExportingProfiles = "Exporting profiles...";
    
    /// <summary>
    /// Message displayed when importing profiles.
    /// </summary>
    public const string ImportingProfiles = "Importing profiles...";
    
    #endregion

    #region Port Operations Messages
    
    /// <summary>
    /// Message displayed when scanning for ports.
    /// </summary>
    public const string ScanningForPorts = "Scanning for ports...";
    
    /// <summary>
    /// Message displayed when testing a port.
    /// </summary>
    public const string TestingPort = "Testing port...";
    
    /// <summary>
    /// Message displayed when an error occurs during port testing.
    /// </summary>
    public const string ErrorTestingPort = "Error testing port";
    
    #endregion

    #region Directory Operations Messages
    
    /// <summary>
    /// Message displayed when opening a profiles folder.
    /// </summary>
    public const string OpeningProfilesFolder = "Opening profiles folder...";
    
    /// <summary>
    /// Message displayed when a profiles folder is opened.
    /// </summary>
    public const string ProfilesFolderOpened = "Profiles folder opened";
    
    /// <summary>
    /// Message displayed when profiles path is updated.
    /// </summary>
    public const string ProfilesPathUpdated = "Profiles path updated";
    
    #endregion

    #region Error Messages
    
    /// <summary>
    /// Message displayed when an error occurs during profile save.
    /// </summary>
    public const string ErrorSavingProfile = "Error saving profile";
    
    /// <summary>
    /// Message displayed when an error occurs during profile load.
    /// </summary>
    public const string ErrorLoadingProfiles = "Error loading profiles";
    
    /// <summary>
    /// Message displayed when an error occurs setting default profile.
    /// </summary>
    public const string ErrorSettingDefaultProfile = "Error setting default profile";
    
    /// <summary>
    /// Message displayed when an error occurs duplicating a profile.
    /// </summary>
    public const string ErrorDuplicatingProfile = "Error duplicating profile";
    
    /// <summary>
    /// Message displayed when an error occurs deleting a profile.
    /// </summary>
    public const string ErrorDeletingProfile = "Error deleting profile";
    
    /// <summary>
    /// Message displayed when an error occurs exporting profiles.
    /// </summary>
    public const string ErrorExportingProfiles = "Error exporting profiles";
    
    /// <summary>
    /// Message displayed when an error occurs importing profiles.
    /// </summary>
    public const string ErrorImportingProfiles = "Error importing profiles";
    
    /// <summary>
    /// Message displayed when an error occurs opening profiles folder.
    /// </summary>
    public const string ErrorOpeningProfilesFolder = "Error opening profiles folder";
    
    /// <summary>
    /// Message displayed when an error occurs resetting profiles path.
    /// </summary>
    public const string ErrorResettingProfilesPath = "Error resetting profiles path";
    
    /// <summary>
    /// Message displayed when an error occurs browsing for profiles path.
    /// </summary>
    public const string ErrorBrowsingForProfilesPath = "Error browsing for profiles path";
    
    /// <summary>
    /// Message displayed when an error occurs validating configuration.
    /// </summary>
    public const string ErrorValidatingConfiguration = "Error validating configuration";
    
    #endregion
}
