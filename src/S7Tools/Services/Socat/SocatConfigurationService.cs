using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;

namespace S7Tools.Services.Socat;

/// <summary>
/// Configures serial devices for socat usage.
/// Single Responsibility: Serial device preparation and validation.
/// </summary>
public class SocatConfigurationService
{
    private readonly ILogger<SocatConfigurationService> _logger;
    private readonly ISerialPortService _serialPortService;

    public SocatConfigurationService(
        ILogger<SocatConfigurationService> logger,
        ISerialPortService serialPortService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
    }

    /// <summary>
    /// Prepares serial device with necessary stty configuration.
    /// </summary>
    public async Task<bool> PrepareSerialDeviceAsync(
        string serialDevice,
        SocatConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        try
        {
            // Validate device exists
            if (!File.Exists(serialDevice))
            {
                _logger.LogError("Serial device {Device} does not exist", serialDevice);
                return false;
            }

            _logger.LogDebug("Preparing serial device {Device} for socat", serialDevice);

            // Validate device accessibility first
            SerialDeviceValidationResult validation = await ValidateSerialDeviceAsync(serialDevice, cancellationToken).ConfigureAwait(false);
            if (!validation.IsValid)
            {
                _logger.LogError("Serial device validation failed for {Device}: {Errors}",
                    serialDevice, string.Join(", ", validation.Errors));
                return false;
            }

            // Create serial port configuration for socat use (raw mode)
            var serialConfig = new SerialPortConfiguration
            {
                BaudRate = configuration.BaudRate,
                CharacterSize = 8,
                Parity = ParityMode.Even,
                StopBits = StopBits.One,
                DisableHardwareFlowControl = true,
                RawMode = true
            };

            // Apply configuration using serial port service
            bool configured = await _serialPortService.ApplyConfigurationAsync(
                serialDevice,
                serialConfig,
                null,
                cancellationToken).ConfigureAwait(false);

            if (configured)
            {
                _logger.LogDebug("Successfully prepared serial device {Device} for socat", serialDevice);
            }
            else
            {
                _logger.LogWarning("Failed to prepare serial device {Device}", serialDevice);
            }

            return configured;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing serial device {Device}", serialDevice);
            return false;
        }
    }

    /// <summary>
    /// Validates serial device for socat compatibility.
    /// </summary>
    public async Task<SerialDeviceValidationResult> ValidateSerialDeviceAsync(
        string serialDevice,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // For async signature consistency

        var result = new SerialDeviceValidationResult();

        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            result.Errors.Add("Serial device path is required");
            result.IsValid = false;
            return result;
        }

        try
        {
            // Check existence
            result.Exists = File.Exists(serialDevice);
            if (!result.Exists)
            {
                result.Errors.Add($"Device {serialDevice} does not exist");
                result.IsValid = false;
                return result;
            }

            // Check accessibility (read/write permissions)
            try
            {
                using var stream = File.Open(serialDevice, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                result.IsAccessible = true;
                _logger.LogDebug("Serial device {Device} is accessible", serialDevice);
            }
            catch (UnauthorizedAccessException)
            {
                result.IsAccessible = false;
                result.Errors.Add($"Insufficient permissions to access {serialDevice}. Try running with elevated privileges.");
                result.IsValid = false;
            }
            catch (IOException ex)
            {
                result.IsAccessible = false;
                result.IsInUse = true;
                result.Warnings.Add($"Device may be in use: {ex.Message}");
            }

            // Get device information if accessible
            if (result.IsAccessible)
            {
                try
                {
                    var fileInfo = new FileInfo(serialDevice);
                    result.DeviceInfo = $"Device: {serialDevice}, Size: {fileInfo.Length} bytes, Last Modified: {fileInfo.LastWriteTime}";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not retrieve device information for {Device}", serialDevice);
                }
            }

            result.IsValid = result.Errors.Count == 0;

            _logger.LogDebug("Validated serial device {Device}: IsValid={IsValid}, Exists={Exists}, IsAccessible={IsAccessible}",
                serialDevice, result.IsValid, result.Exists, result.IsAccessible);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating serial device {Device}", serialDevice);
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
            return result;
        }
    }
}
