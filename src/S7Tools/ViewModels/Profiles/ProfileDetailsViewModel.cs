using System.Collections.ObjectModel;
using ReactiveUI;
using S7Tools.Core.Services.Interfaces;

namespace S7Tools.ViewModels.Profiles;

/// <summary>
/// Base implementation of profile details display ViewModel
/// </summary>
public class ProfileDetailsViewModel : ReactiveObject, IProfileDetailsViewModel
{
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileDetailsViewModel"/> class.
    /// </summary>
    /// <param name="profileName">The profile name.</param>
    /// <param name="profileType">The profile type description.</param>
    /// <param name="basicProperties">The basic properties collection.</param>
    /// <param name="configurationProperties">The configuration properties collection.</param>
    /// <param name="advancedProperties">The advanced properties collection.</param>
    /// <param name="isValid">Whether the profile data is valid.</param>
    /// <param name="validationMessage">The validation message if any.</param>
    /// <param name="isMissing">Whether this represents a missing profile.</param>
    public ProfileDetailsViewModel(
        string profileName,
        string profileType,
        ObservableCollection<PropertyDisplayItem> basicProperties,
        ObservableCollection<PropertyDisplayItem> configurationProperties,
        ObservableCollection<PropertyDisplayItem> advancedProperties,
        bool isValid,
        string? validationMessage,
        bool isMissing)
    {
        ProfileName = profileName ?? throw new ArgumentNullException(nameof(profileName));
        ProfileType = profileType ?? throw new ArgumentNullException(nameof(profileType));
        BasicProperties = basicProperties ?? throw new ArgumentNullException(nameof(basicProperties));
        ConfigurationProperties = configurationProperties ?? throw new ArgumentNullException(nameof(configurationProperties));
        AdvancedProperties = advancedProperties ?? throw new ArgumentNullException(nameof(advancedProperties));
        IsValid = isValid;
        ValidationMessage = validationMessage;
        IsMissing = isMissing;
    }

    /// <inheritdoc />
    public string ProfileName { get; private set; }

    /// <inheritdoc />
    public string ProfileType { get; private set; }

    /// <inheritdoc />
    public ObservableCollection<PropertyDisplayItem> BasicProperties { get; private set; }

    /// <inheritdoc />
    public ObservableCollection<PropertyDisplayItem> ConfigurationProperties { get; private set; }

    /// <inheritdoc />
    public ObservableCollection<PropertyDisplayItem> AdvancedProperties { get; private set; }

    /// <inheritdoc />
    public bool IsValid { get; private set; }

    /// <inheritdoc />
    public string? ValidationMessage { get; private set; }

    /// <inheritdoc />
    public bool IsMissing { get; private set; }

    /// <inheritdoc />
    public async Task UpdateProfileAsync(IProfileBase? profile)
    {
        await Task.Run(() =>
        {
            if (profile == null)
            {
                UpdateToEmptyState();
                return;
            }

            ProfileName = profile.Name;
            IsValid = !string.IsNullOrWhiteSpace(profile.Name);
            ValidationMessage = IsValid ? null : "Profile name is required";
            IsMissing = false;

            // Update basic properties
            UpdateBasicProperties(profile);

            // Note: For a complete implementation, we would need access to the ProfileDetailsService
            // to regenerate configuration and advanced properties. This is a simplified update.
        });
    }

    /// <summary>
    /// Updates the basic properties with profile information.
    /// </summary>
    /// <param name="profile">The profile to update from.</param>
    protected virtual void UpdateBasicProperties(IProfileBase profile)
    {
        // Update existing basic properties
        PropertyDisplayItem? nameProperty = BasicProperties.FirstOrDefault(p => p.Label == "Name");
        if (nameProperty != null)
        {
            nameProperty.Value = profile.Name;
            nameProperty.ValidationState = string.IsNullOrWhiteSpace(profile.Name)
                ? PropertyValidationState.Error
                : PropertyValidationState.Valid;
        }

        PropertyDisplayItem? idProperty = BasicProperties.FirstOrDefault(p => p.Label == "ID");
        if (idProperty != null)
        {
            idProperty.Value = profile.Id.ToString("D");
        }

        PropertyDisplayItem? descriptionProperty = BasicProperties.FirstOrDefault(p => p.Label == "Description");
        if (descriptionProperty != null)
        {
            descriptionProperty.Value = string.IsNullOrWhiteSpace(profile.Description)
                ? "No description"
                : profile.Description;
        }
    }

    /// <summary>
    /// Updates the ViewModel to represent an empty/no profile state.
    /// </summary>
    protected virtual void UpdateToEmptyState()
    {
        ProfileName = "No Profile Selected";
        IsValid = false;
        ValidationMessage = "No profile selected";
        IsMissing = false;

        BasicProperties.Clear();
        ConfigurationProperties.Clear();
        AdvancedProperties.Clear();

        BasicProperties.Add(new PropertyDisplayItem
        {
            Label = "Status",
            Value = "No profile selected",
            ValidationState = PropertyValidationState.Warning
        });
    }

    /// <summary>
    /// Raises property changed events for UI binding updates.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    protected void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        this.RaisePropertyChanged(propertyName);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Clear collections to help with garbage collection
            BasicProperties.Clear();
            ConfigurationProperties.Clear();
            AdvancedProperties.Clear();

            _disposed = true;
        }
    }
}
