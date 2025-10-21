using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.ViewModels.Profiles;

namespace S7Tools.Services;

/// <summary>
/// Service for creating formatted profile details ViewModels.
/// Handles different profile types and creates consistent display information.
/// </summary>
public class ProfileDetailsService : IProfileDetailsService
{
    private readonly ILogger<ProfileDetailsService> _logger;

    public ProfileDetailsService(ILogger<ProfileDetailsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a profile details ViewModel for the specified profile
    /// </summary>
    /// <typeparam name="T">Profile type implementing IProfileBase</typeparam>
    /// <param name="profile">Profile to create details for</param>
    /// <returns>Profile details ViewModel or null if profile is null</returns>
    public IProfileDetailsViewModel? CreateProfileDetailsViewModel<T>(T? profile) where T : class, IProfileBase
    {
        if (profile == null)
        {
            return null;
        }

        try
        {
            var basicProperties = new ObservableCollection<PropertyDisplayItem>();
            var configurationProperties = new ObservableCollection<PropertyDisplayItem>();
            var advancedProperties = new ObservableCollection<PropertyDisplayItem>();

            // Add basic profile information
            basicProperties.Add(new PropertyDisplayItem
            {
                Label = "Profile ID",
                Value = profile.Id.ToString(),
                Tooltip = "Unique identifier for this profile"
            });

            basicProperties.Add(new PropertyDisplayItem
            {
                Label = "Name",
                Value = profile.Name,
                Tooltip = "Display name for this profile"
            });

            if (!string.IsNullOrEmpty(profile.Description))
            {
                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Description",
                    Value = profile.Description,
                    Tooltip = "Profile description"
                });
            }

            // Add profile flags
            if (profile.IsDefault)
            {
                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Default Profile",
                    Value = "Yes",
                    Tooltip = "This is the default profile"
                });
            }

            if (profile.IsReadOnly)
            {
                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Read Only",
                    Value = "Yes",
                    Tooltip = "This profile cannot be modified"
                });
            }

            // Add type-specific configuration properties
            AddConfigurationProperties(profile, configurationProperties);

            bool isValid = !string.IsNullOrWhiteSpace(profile.Name);
            string? validationMessage = isValid ? null : "Profile name is required";

            return new ProfileDetailsViewModel(
                profile.Name,
                GetProfileTypeName(profile),
                basicProperties,
                configurationProperties,
                advancedProperties,
                isValid,
                validationMessage,
                false
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating profile details for {ProfileType} profile {ProfileName}",
                typeof(T).Name, profile?.Name ?? "Unknown");
            return CreateEmptyViewModel($"Error loading profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates profile details ViewModel with error handling for missing profiles
    /// </summary>
    /// <param name="profileId">Profile ID to load</param>
    /// <param name="profileType">Type of profile expected</param>
    /// <param name="profileManager">Manager to load profile data</param>
    /// <returns>Profile details ViewModel with error state if loading fails</returns>
    public async Task<IProfileDetailsViewModel> CreateProfileDetailsViewModelAsync<T>(
        Guid? profileId,
        string profileType,
        IProfileManager<T> profileManager)
        where T : class, IProfileBase
    {
        if (!profileId.HasValue)
        {
            return CreateEmptyViewModel("No profile ID provided");
        }

        try
        {
            T? profile = await profileManager.GetByIdAsync(Convert.ToInt32(profileId.Value.GetHashCode()));
            if (profile == null)
            {
                return CreateEmptyViewModel($"Profile with ID {profileId} not found");
            }

            IProfileDetailsViewModel? result = CreateProfileDetailsViewModel(profile);
            return result ?? CreateEmptyViewModel($"Error creating profile details");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profile details for profile ID {ProfileId}", profileId);
            return CreateEmptyViewModel($"Error loading profile: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats a property value for display with appropriate type handling
    /// </summary>
    /// <param name="value">Property value to format</param>
    /// <param name="propertyType">Type of the property</param>
    /// <returns>Formatted string representation</returns>
    public string FormatPropertyValue(object? value, Type propertyType)
    {
        if (value == null)
        {
            return "Not set";
        }

        return value switch
        {
            string str when string.IsNullOrEmpty(str) => "Not set",
            string str => str,
            bool b => b ? "Yes" : "No",
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            TimeSpan ts => FormatTimeSpan(ts),
            _ => value.ToString() ?? "Unknown"
        };
    }

    /// <summary>
    /// Validates profile data and returns validation state
    /// </summary>
    /// <typeparam name="T">Profile type</typeparam>
    /// <param name="profile">Profile to validate</param>
    /// <returns>Validation result with state and messages</returns>
    public ProfileDisplayValidationResult ValidateProfileData<T>(T profile) where T : class, IProfileBase
    {
        // Simplified validation for now - we'll implement full validation later
        if (profile == null || string.IsNullOrWhiteSpace(profile.Name))
        {
            return new ProfileDisplayValidationResult
            {
                State = ProfileValidationState.Error,
                Messages = new[] { profile == null ? "Profile is null" : "Profile name is required" }
            };
        }

        return new ProfileDisplayValidationResult
        {
            State = ProfileValidationState.Valid,
            Messages = Array.Empty<string>()
        };
    }

    private void AddConfigurationProperties<T>(T profile, ObservableCollection<PropertyDisplayItem> properties) where T : class, IProfileBase
    {
        switch (profile)
        {
            case SerialPortProfile serialProfile:
                AddSerialPortProperties(serialProfile, properties);
                break;
            case SocatProfile socatProfile:
                AddSocatProperties(socatProfile, properties);
                break;
            case PowerSupplyProfile powerProfile:
                AddPowerSupplyProperties(powerProfile, properties);
                break;
            default:
                properties.Add(new PropertyDisplayItem
                {
                    Label = "Configuration",
                    Value = "Custom configuration type",
                    Tooltip = $"Configuration type: {profile.GetType().Name}"
                });
                break;
        }
    }

    private static void AddSerialPortProperties(SerialPortProfile profile, ObservableCollection<PropertyDisplayItem> properties)
    {
        if (profile.Configuration == null)
        {
            properties.Add(new PropertyDisplayItem
            {
                Label = "Configuration",
                Value = "Not configured",
                ValidationState = PropertyValidationState.Error
            });
            return;
        }

        SerialPortConfiguration config = profile.Configuration;
        properties.Add(new PropertyDisplayItem
        {
            Label = "Baud Rate",
            Value = config.BaudRate.ToString(),
            Tooltip = "Serial communication speed"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Character Size",
            Value = $"{config.CharacterSize} bits",
            Tooltip = "Number of data bits per character"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Parity",
            Value = config.Parity.ToString(),
            Tooltip = "Parity checking method"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Stop Bits",
            Value = config.StopBits.ToString(),
            Tooltip = "Number of stop bits"
        });
    }

    private static void AddSocatProperties(SocatProfile profile, ObservableCollection<PropertyDisplayItem> properties)
    {
        if (profile.Configuration == null)
        {
            properties.Add(new PropertyDisplayItem
            {
                Label = "Configuration",
                Value = "Not configured",
                ValidationState = PropertyValidationState.Error
            });
            return;
        }

        SocatConfiguration config = profile.Configuration;
        properties.Add(new PropertyDisplayItem
        {
            Label = "TCP Port",
            Value = config.TcpPort.ToString(),
            Tooltip = "TCP port for socat bridge"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Host",
            Value = string.IsNullOrEmpty(config.TcpHost) ? "localhost" : config.TcpHost,
            Tooltip = "Host address for TCP connection"
        });
    }

    private static void AddPowerSupplyProperties(PowerSupplyProfile profile, ObservableCollection<PropertyDisplayItem> properties)
    {
        if (profile.Configuration == null)
        {
            properties.Add(new PropertyDisplayItem
            {
                Label = "Configuration",
                Value = "Not configured",
                ValidationState = PropertyValidationState.Error
            });
            return;
        }

        PowerSupplyConfiguration config = profile.Configuration;

        // Add common configuration properties
        properties.Add(new PropertyDisplayItem
        {
            Label = "Device Type",
            Value = config.GetType().Name.Replace("Configuration", ""),
            Tooltip = "Power supply communication protocol"
        });

        // Add type-specific properties based on configuration type
        if (config is ModbusTcpConfiguration tcpConfig)
        {
            properties.Add(new PropertyDisplayItem
            {
                Label = "Host",
                Value = tcpConfig.Host,
                Tooltip = "Power supply IP address or hostname"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Port",
                Value = tcpConfig.Port.ToString(),
                Tooltip = "TCP port for Modbus communication"
            });
        }
    }

    private static string GetProfileTypeName<T>(T profile) where T : class, IProfileBase
    {
        return profile switch
        {
            SerialPortProfile => "Serial Port",
            SocatProfile => "Socat Bridge",
            PowerSupplyProfile => "Power Supply",
            _ => "Unknown Profile Type"
        };
    }

    private static ProfileDetailsViewModel CreateEmptyViewModel(string message)
    {
        var basicProperties = new ObservableCollection<PropertyDisplayItem>
        {
            new PropertyDisplayItem
            {
                Label = "Status",
                Value = message,
                ValidationState = PropertyValidationState.Warning
            }
        };

        return new ProfileDetailsViewModel(
            "No Profile",
            "None",
            basicProperties,
            new ObservableCollection<PropertyDisplayItem>(),
            new ObservableCollection<PropertyDisplayItem>(),
            false,
            message,
            false
        );
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1)
        {
            return $"{timeSpan.TotalDays:F1} days";
        }
        if (timeSpan.TotalHours >= 1)
        {
            return $"{timeSpan.TotalHours:F1} hours";
        }
        if (timeSpan.TotalMinutes >= 1)
        {
            return $"{timeSpan.TotalMinutes:F1} minutes";
        }
        if (timeSpan.TotalSeconds >= 1)
        {
            return $"{timeSpan.TotalSeconds:F1} seconds";
        }
        return $"{timeSpan.TotalMilliseconds:F0} ms";
    }
}
