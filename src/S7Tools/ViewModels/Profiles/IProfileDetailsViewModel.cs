using System.Collections.ObjectModel;
using S7Tools.Core.Services.Interfaces;
using S7Tools.ViewModels.Controls;

namespace S7Tools.ViewModels.Profiles;

/// <summary>
/// Interface for profile details display ViewModels
/// </summary>
public interface IProfileDetailsViewModel : IDisposable
{
    /// <summary>
    /// Display name of the profile
    /// </summary>
    string ProfileName { get; }

    /// <summary>
    /// Human-readable profile type (e.g., "Serial Port", "Socat Bridge")
    /// </summary>
    string ProfileType { get; }

    /// <summary>
    /// Basic configuration properties for display
    /// </summary>
    ObservableCollection<PropertyDisplayItem> BasicProperties { get; }

    /// <summary>
    /// Main configuration properties for display
    /// </summary>
    ObservableCollection<PropertyDisplayItem> ConfigurationProperties { get; }

    /// <summary>
    /// Advanced/optional properties for display
    /// </summary>
    ObservableCollection<PropertyDisplayItem> AdvancedProperties { get; }

    /// <summary>
    /// True if profile data is valid and complete
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Error or warning message if profile is invalid
    /// </summary>
    string? ValidationMessage { get; }

    /// <summary>
    /// True if this represents a missing/deleted profile
    /// </summary>
    bool IsMissing { get; }

    /// <summary>
    /// Updates the profile data and refreshes display properties
    /// </summary>
    /// <param name="profile">New profile data</param>
    Task UpdateProfileAsync(IProfileBase? profile);
}
