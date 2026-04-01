using System.Text.RegularExpressions;
using S7Tools.Core.Constants;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Models;

namespace S7Tools.Services.Socat;

/// <summary>
/// Builds and validates socat command strings.
/// Single Responsibility: Command generation and validation.
/// </summary>
public partial class SocatCommandBuilder
{
    private readonly ILogger<SocatCommandBuilder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocatCommandBuilder"/> class.
    /// </summary>
    public SocatCommandBuilder(ILogger<SocatCommandBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates socat command from configuration.
    /// </summary>
    public string GenerateCommand(SocatConfiguration configuration, string serialDevice)
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        _logger.LogDebug("Generating socat command for device {Device} on port {Port}",
            serialDevice, configuration.TcpPort);

        // Use configuration's built-in command generation
        return configuration.GenerateCommand(serialDevice);
    }

    /// <summary>
    /// Generates socat command from profile.
    /// </summary>
    public string GenerateCommandForProfile(SocatProfile profile, string serialDevice)
    {
        ArgumentNullException.ThrowIfNull(profile, nameof(profile));
        if (string.IsNullOrWhiteSpace(serialDevice))
        {
            throw new ArgumentException("Serial device cannot be null or empty", nameof(serialDevice));
        }

        _logger.LogDebug("Generating socat command for profile '{Profile}' with device {Device}",
            profile.Name, serialDevice);

        return profile.Configuration.GenerateCommand(serialDevice);
    }

    /// <summary>
    /// Validates a socat command for syntax and security.
    /// </summary>
    public SocatCommandValidationResult Validate(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var result = new SocatCommandValidationResult
        {
            ValidatedCommand = command.Trim()
        };

        try
        {
            // Basic command structure validation
            if (!command.TrimStart().StartsWith("socat", StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add("Command must start with 'socat'");
                result.IsValid = false;
                return result;
            }

            // Check for required TCP-LISTEN part
            Match tcpListenMatch = TcpListenRegex().Match(command);
            if (!tcpListenMatch.Success)
            {
                result.Errors.Add("Command must contain TCP-LISTEN:port specification");
                result.IsValid = false;
            }
            else
            {
                if (int.TryParse(tcpListenMatch.Groups[1].Value, out int port))
                {
                    if (!NetworkConstants.IsValidPort(port))
                    {
                        result.Errors.Add(string.Format(NetworkConstants.PortRangeError, port));
                        result.IsValid = false;
                    }
                    else
                    {
                        result.DetectedTcpPort = port;
                    }
                }
            }

            // Check for serial device specification
            Match deviceMatch = SerialDeviceRegex().Match(command);
            if (!deviceMatch.Success)
            {
                result.Errors.Add("Command must contain a serial device path (/dev/...)");
                result.IsValid = false;
            }
            else
            {
                result.DetectedSerialDevice = deviceMatch.Groups[1].Value;
            }

            // Check for potentially dangerous flags
            if (command.Contains("-r", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add("Command contains raw mode flag (-r) which may affect performance");
            }

            // Check if command requires root privileges (ports < 1024)
            if (result.DetectedTcpPort.HasValue && result.DetectedTcpPort.Value < 1024)
            {
                result.RequiresRoot = true;
                result.Warnings.Add($"TCP port {result.DetectedTcpPort.Value} may require root privileges");
            }

            // If no errors, command is valid
            if (result.Errors.Count == 0)
            {
                result.IsValid = true;
            }

            _logger.LogDebug("Validated socat command: {IsValid}, Port: {Port}, Device: {Device}",
                result.IsValid, result.DetectedTcpPort, result.DetectedSerialDevice);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating socat command: {Command}", command);
            result.Errors.Add($"Validation error: {ex.Message}");
            result.IsValid = false;
            return result;
        }
    }

    /// <summary>
    /// Extracts TCP port from command using regex.
    /// </summary>
    public int? ExtractTcpPort(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        Match match = TcpListenRegex().Match(command);
        return match.Success && int.TryParse(match.Groups[1].Value, out int port)
            ? port
            : null;
    }

    /// <summary>
    /// Extracts serial device path from command.
    /// </summary>
    public string? ExtractSerialDevice(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        Match match = SerialDeviceRegex().Match(command);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Regex pattern for extracting TCP-LISTEN port.
    /// </summary>
    [GeneratedRegex(@"TCP-LISTEN:(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TcpListenRegex();

    /// <summary>
    /// Regex pattern for extracting serial device path.
    /// </summary>
    [GeneratedRegex(@"(/dev/[^\s,]+)")]
    private static partial Regex SerialDeviceRegex();
}
