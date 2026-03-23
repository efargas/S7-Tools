using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Constants;
using S7Tools.Core.Models;
using S7Tools.Core.Interfaces.Services;
using S7Tools.ViewModels.Controls;
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

            // Add Options if present
            if (!string.IsNullOrEmpty(profile.Options))
            {
                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Options",
                    Value = profile.Options,
                    Tooltip = "Additional command-line options or configuration settings"
                });
            }

            // Add Flags if present
            if (!string.IsNullOrEmpty(profile.Flags))
            {
                basicProperties.Add(new PropertyDisplayItem
                {
                    Label = "Flags",
                    Value = profile.Flags,
                    Tooltip = "Additional flags or parameters for profile-specific operations"
                });
            }

            // Add metadata
            basicProperties.Add(new PropertyDisplayItem
            {
                Label = "Version",
                Value = profile.Version,
                Tooltip = "Profile format version"
            });

            basicProperties.Add(new PropertyDisplayItem
            {
                Label = "Created",
                Value = profile.CreatedAt.ToString(DateTimeFormats.LongDateTime),
                Tooltip = "When this profile was created"
            });

            basicProperties.Add(new PropertyDisplayItem
            {
                Label = "Modified",
                Value = profile.ModifiedAt.ToString(DateTimeFormats.LongDateTime),
                Tooltip = "When this profile was last modified"
            });

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
            DateTime dt => dt.ToString(DateTimeFormats.LongDateTime),
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

        // Basic Serial Settings
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

        // Control Flags
        properties.Add(new PropertyDisplayItem
        {
            Label = "Enable Receiver",
            Value = config.EnableReceiver ? "Yes" : "No",
            Tooltip = "Enable receiver (CREAD flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Hardware Flow Control",
            Value = config.DisableHardwareFlowControl ? "Disabled" : "Enabled",
            Tooltip = "Hardware flow control (-crtscts flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Parity Enabled",
            Value = config.ParityEnabled ? "Yes" : "No",
            Tooltip = "Parity checking enabled (parenb flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Odd Parity",
            Value = config.OddParity ? "Yes" : "No",
            Tooltip = "Use odd parity (parodd flag)"
        });

        // Input Flags
        properties.Add(new PropertyDisplayItem
        {
            Label = "Ignore Break",
            Value = config.IgnoreBreak ? "Yes" : "No",
            Tooltip = "Ignore break conditions (ignbrk flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Break Interrupt",
            Value = config.DisableBreakInterrupt ? "Disabled" : "Enabled",
            Tooltip = "Signal interrupt on break (-brkint flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "CR to NL Mapping",
            Value = config.DisableMapCRtoNL ? "Disabled" : "Enabled",
            Tooltip = "Map CR to NL on input (-icrnl flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Bell on Queue Full",
            Value = config.DisableBellOnQueueFull ? "Disabled" : "Enabled",
            Tooltip = "Ring bell on input queue full (-imaxbel flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "XON/XOFF Flow Control",
            Value = config.DisableXonXoffFlowControl ? "Disabled" : "Enabled",
            Tooltip = "XON/XOFF flow control (-ixon flag)"
        });

        // Output Flags
        properties.Add(new PropertyDisplayItem
        {
            Label = "Output Processing",
            Value = config.DisableOutputProcessing ? "Disabled" : "Enabled",
            Tooltip = "Output processing (-opost flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "NL to CR-NL Mapping",
            Value = config.DisableMapNLtoCRNL ? "Disabled" : "Enabled",
            Tooltip = "Map NL to CR-NL on output (-onlcr flag)"
        });

        // Local Flags
        properties.Add(new PropertyDisplayItem
        {
            Label = "Canonical Mode",
            Value = config.DisableCanonicalMode ? "Disabled" : "Enabled",
            Tooltip = "Canonical input processing (-icanon flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Signal Generation",
            Value = config.DisableSignalGeneration ? "Disabled" : "Enabled",
            Tooltip = "Signal generation (-isig flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Extended Processing",
            Value = config.DisableExtendedProcessing ? "Disabled" : "Enabled",
            Tooltip = "Extended input processing (-iexten flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Echo",
            Value = config.DisableEcho ? "Disabled" : "Enabled",
            Tooltip = "Echo input characters (-echo flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Echo Erase",
            Value = config.DisableEchoErase ? "Disabled" : "Enabled",
            Tooltip = "Echo erase characters (-echoe flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Echo Kill",
            Value = config.DisableEchoKill ? "Disabled" : "Enabled",
            Tooltip = "Echo kill characters (-echok flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Echo Control",
            Value = config.DisableEchoControl ? "Disabled" : "Enabled",
            Tooltip = "Echo control characters (-echoctl flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Echo Kill Erase",
            Value = config.DisableEchoKillErase ? "Disabled" : "Enabled",
            Tooltip = "Echo kill with erase (-echoke flag)"
        });

        // Special Modes
        properties.Add(new PropertyDisplayItem
        {
            Label = "Raw Mode",
            Value = config.RawMode ? "Enabled" : "Disabled",
            Tooltip = "Raw mode disables all input and output processing"
        });

        // Metadata
        properties.Add(new PropertyDisplayItem
        {
            Label = "Config Version",
            Value = config.Version,
            Tooltip = "Configuration format version"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Config Created",
            Value = config.CreatedAt.ToString(DateTimeFormats.LongDateTime),
            Tooltip = "When this configuration was created"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Config Modified",
            Value = config.ModifiedAt.ToString(DateTimeFormats.LongDateTime),
            Tooltip = "When this configuration was last modified"
        });

        // Additional metadata if present
        if (config.Metadata != null && config.Metadata.Count > 0)
        {
            foreach (KeyValuePair<string, string> metadata in config.Metadata)
            {
                properties.Add(new PropertyDisplayItem
                {
                    Label = $"Metadata: {metadata.Key}",
                    Value = metadata.Value,
                    Tooltip = "Additional configuration metadata"
                });
            }
        }
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

        // TCP Settings
        properties.Add(new PropertyDisplayItem
        {
            Label = "TCP Port",
            Value = config.TcpPort.ToString(),
            Tooltip = "TCP port for socat bridge"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "TCP Host",
            Value = string.IsNullOrEmpty(config.TcpHost) ? "All interfaces" : config.TcpHost,
            Tooltip = "Host address for TCP connection"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Fork Mode",
            Value = config.EnableFork ? "Enabled" : "Disabled",
            Tooltip = "Allow multiple concurrent connections"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Reuse Address",
            Value = config.EnableReuseAddr ? "Enabled" : "Disabled",
            Tooltip = "Enable address reuse option"
        });

        // Socat Flags
        properties.Add(new PropertyDisplayItem
        {
            Label = "Verbose Logging",
            Value = config.Verbose ? "Enabled" : "Disabled",
            Tooltip = "Enable verbose mode (-v flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Hex Dump",
            Value = config.HexDump ? "Enabled" : "Disabled",
            Tooltip = "Enable hex dump of transferred data (-x flag)"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Block Size",
            Value = $"{config.BlockSize} bytes",
            Tooltip = "Block size for data transfers"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Debug Level",
            Value = config.DebugLevel.ToString(),
            Tooltip = "Debug level (number of -d flags)"
        });

        // Serial Device Settings
        properties.Add(new PropertyDisplayItem
        {
            Label = "Serial Raw Mode",
            Value = config.SerialRawMode ? "Enabled" : "Disabled",
            Tooltip = "Enable raw mode for the serial device"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Serial Echo",
            Value = config.SerialDisableEcho ? "Disabled" : "Enabled",
            Tooltip = "Echo on the serial device"
        });

        // Process Management
        properties.Add(new PropertyDisplayItem
        {
            Label = "Auto Configure Serial",
            Value = config.AutoConfigureSerial ? "Yes" : "No",
            Tooltip = "Automatically configure serial port with stty before starting socat"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Connection Timeout",
            Value = config.ConnectionTimeout == 0 ? "No timeout" : $"{config.ConnectionTimeout} seconds",
            Tooltip = "Timeout for TCP connections"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Auto Restart",
            Value = config.AutoRestart ? "Enabled" : "Disabled",
            Tooltip = "Restart socat automatically if it terminates unexpectedly"
        });

        // Metadata
        properties.Add(new PropertyDisplayItem
        {
            Label = "Config Version",
            Value = config.Version,
            Tooltip = "Configuration format version"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Config Created",
            Value = config.CreatedAt.ToString(DateTimeFormats.LongDateTime),
            Tooltip = "When this configuration was created"
        });

        properties.Add(new PropertyDisplayItem
        {
            Label = "Config Modified",
            Value = config.ModifiedAt.ToString(DateTimeFormats.LongDateTime),
            Tooltip = "When this configuration was last modified"
        });

        // Additional metadata if present
        if (config.Metadata != null && config.Metadata.Count > 0)
        {
            foreach (KeyValuePair<string, string> metadata in config.Metadata)
            {
                properties.Add(new PropertyDisplayItem
                {
                    Label = $"Metadata: {metadata.Key}",
                    Value = metadata.Value,
                    Tooltip = "Additional configuration metadata"
                });
            }
        }
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

        // Common configuration properties
        properties.Add(new PropertyDisplayItem
        {
            Label = "Device Type",
            Value = config.Type.ToString(),
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

            properties.Add(new PropertyDisplayItem
            {
                Label = "Device ID",
                Value = tcpConfig.DeviceId.ToString(),
                Tooltip = "Modbus device/slave ID"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "On/Off Coil",
                Value = tcpConfig.OnOffCoil.ToString(),
                Tooltip = "Modbus coil address for power on/off control"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Addressing Mode",
                Value = tcpConfig.AddressingMode.ToString(),
                Tooltip = "Modbus addressing mode (Base0 or Base1)"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Connection Timeout",
                Value = $"{tcpConfig.ConnectionTimeoutMs} ms",
                Tooltip = "Timeout for establishing connection"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Read Timeout",
                Value = $"{tcpConfig.ReadTimeoutMs} ms",
                Tooltip = "Timeout for read operations"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Write Timeout",
                Value = $"{tcpConfig.WriteTimeoutMs} ms",
                Tooltip = "Timeout for write operations"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Auto Reconnect",
                Value = tcpConfig.EnableAutoReconnect ? "Enabled" : "Disabled",
                Tooltip = "Automatically reconnect on connection loss"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Max Retry Attempts",
                Value = tcpConfig.MaxRetryAttempts.ToString(),
                Tooltip = "Maximum number of retry attempts for operations"
            });

            properties.Add(new PropertyDisplayItem
            {
                Label = "Connection String",
                Value = tcpConfig.GenerateConnectionString(),
                Tooltip = "Generated connection string for this configuration"
            });
        }
        // Note: RTU and other configurations can be added here when implemented
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
