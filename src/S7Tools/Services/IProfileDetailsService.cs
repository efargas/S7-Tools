using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.ViewModels.Profiles;

namespace S7Tools.Services;

/// <summary>
/// Service for creating formatted profile detail ViewModels
/// </summary>
public interface IProfileDetailsService
{
    /// <summary>
    /// Creates a profile details ViewModel for the specified profile
    /// </summary>
    /// <typeparam name="T">Profile type implementing IProfileBase</typeparam>
    /// <param name="profile">Profile to create details for</param>
    /// <returns>Profile details ViewModel or null if profile is null</returns>
    IProfileDetailsViewModel? CreateProfileDetailsViewModel<T>(T? profile)
        where T : class, IProfileBase;

    /// <summary>
    /// Creates profile details ViewModel with error handling for missing profiles
    /// </summary>
    /// <param name="profileId">Profile ID to load</param>
    /// <param name="profileType">Type of profile expected</param>
    /// <param name="profileManager">Manager to load profile data</param>
    /// <returns>Profile details ViewModel with error state if loading fails</returns>
    Task<IProfileDetailsViewModel> CreateProfileDetailsViewModelAsync<T>(
        Guid? profileId,
        string profileType,
        IProfileManager<T> profileManager)
        where T : class, IProfileBase;

    /// <summary>
    /// Formats a property value for display with appropriate type handling
    /// </summary>
    /// <param name="value">Property value to format</param>
    /// <param name="propertyType">Type of the property</param>
    /// <returns>Formatted string representation</returns>
    string FormatPropertyValue(object? value, Type propertyType);

    /// <summary>
    /// Validates profile data and returns validation state
    /// </summary>
    /// <typeparam name="T">Profile type</typeparam>
    /// <param name="profile">Profile to validate</param>
    /// <returns>Validation result with state and messages</returns>
    ProfileDisplayValidationResult ValidateProfileData<T>(T profile)
        where T : class, IProfileBase;
}
