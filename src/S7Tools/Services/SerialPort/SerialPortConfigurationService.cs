using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Exceptions;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Core.Services.Shell;

namespace S7Tools.Services.SerialPort;

/// <summary>
/// Service responsible for serial port configuration management and stty command operations.
/// This service provides stateless configuration read/write, command generation, and validation.
/// </summary>
public sealed partial class SerialPortConfigurationService
{
    private readonly ILogger<SerialPortConfigurationService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IShellCommandExecutor _shellExecutor;

    /// <summary>
    /// Initializes a new instance of the SerialPortConfigurationService class.
    /// </summary>
    /// <param name="logger">The logger instance for structured logging.</param>
    /// <param name="timeProvider">The time provider for abstracting time operations.</param>
    /// <param name="shellExecutor">The shell command executor for running system commands.</param>
    public SerialPortConfigurationService(
        ILogger<SerialPortConfigurationService> logger,
        ITimeProvider timeProvider,
        IShellCommandExecutor shellExecutor)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _shellExecutor = shellExecutor ?? throw new ArgumentNullException(nameof(shellExecutor));
    }

    /// <summary>
    /// Reads the current configuration of a serial port.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The port configuration if successful, null otherwise.</returns>
    public async Task<SerialPortConfiguration?> ReadPortConfigurationAsync(
        string portPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            var arguments = new List<string> { "-F", portPath, "-a" };
            SttyCommandResult result = await ExecuteSttyDirectAsync(arguments, cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                throw new ValidationException(
                    "PortConfiguration",
                    $"Failed to read port configuration: {result.StandardError}");
            }

            return ParseSttyOutput(result.StandardOutput);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read configuration for port {PortPath}", portPath);
            throw;
        }
    }

    /// <summary>
    /// Applies a configuration to a serial port.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="configuration">The configuration to apply.</param>
    /// <param name="taskLogger">Optional task-specific logger.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> ApplyConfigurationAsync(
        string portPath,
        SerialPortConfiguration configuration,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        Microsoft.Extensions.Logging.ILogger effectiveLogger = taskLogger ?? _logger;

        try
        {
            var arguments = GenerateSttyArguments(portPath, configuration);
            effectiveLogger.LogDebug("Executing stty command: stty {Arguments}", string.Join(" ", arguments));

            SttyCommandResult result = await ExecuteSttyDirectAsync(arguments, cancellationToken).ConfigureAwait(false);

            if (result.Success)
            {
                effectiveLogger.LogDebug("Applied configuration to port {PortPath}", portPath);
                return true;
            }
            else
            {
                effectiveLogger.LogError("Failed to apply configuration to port {PortPath}: {Error}", portPath, result.StandardError);
                return false;
            }
        }
        catch (Exception ex)
        {
            effectiveLogger.LogError(ex, "Failed to apply configuration to port {PortPath}", portPath);
            throw;
        }
    }

    /// <summary>
    /// Applies a serial port profile to a port.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="profile">The profile to apply.</param>
    /// <param name="taskLogger">Optional task-specific logger.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> ApplyProfileAsync(
        string portPath,
        SerialPortProfile profile,
        Microsoft.Extensions.Logging.ILogger? taskLogger = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        return await ApplyConfigurationAsync(portPath, profile.Configuration, taskLogger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Backs up the current configuration of a serial port.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The backed up configuration if successful, null otherwise.</returns>
    public async Task<SerialPortConfiguration?> BackupPortConfigurationAsync(
        string portPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        try
        {
            SerialPortConfiguration? configuration = await ReadPortConfigurationAsync(portPath, cancellationToken).ConfigureAwait(false);
            if (configuration != null)
            {
                _logger.LogDebug("Backed up configuration for port {PortPath}", portPath);
            }
            return configuration;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup configuration for port {PortPath}", portPath);
            return null;
        }
    }

    /// <summary>
    /// Restores a backed up configuration to a serial port.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="backupConfiguration">The configuration to restore.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if successful, false otherwise.</returns>
    public async Task<bool> RestorePortConfigurationAsync(
        string portPath,
        SerialPortConfiguration backupConfiguration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(backupConfiguration, nameof(backupConfiguration));

        try
        {
            bool success = await ApplyConfigurationAsync(portPath, backupConfiguration, null, cancellationToken).ConfigureAwait(false);
            if (success)
            {
                _logger.LogInformation("Restored configuration for port {PortPath}", portPath);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore configuration for port {PortPath}", portPath);
            throw;
        }
    }

    /// <summary>
    /// Generates an stty command for a serial port configuration.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="configuration">The configuration to generate the command for.</param>
    /// <returns>The stty command string.</returns>
    public string GenerateSttyCommand(string portPath, SerialPortConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var sb = new StringBuilder();
        sb.Append($"stty -F '{portPath.Replace("'", "'\"'\"'")}'");

        // Character size
        sb.Append($" cs{configuration.CharacterSize}");

        // Baud rate
        sb.Append($" {configuration.BaudRate}");

        // Input flags
        sb.Append(configuration.IgnoreBreak ? " ignbrk" : " -ignbrk");
        sb.Append(configuration.DisableBreakInterrupt ? " -brkint" : " brkint");
        sb.Append(configuration.DisableMapCRtoNL ? " -icrnl" : " icrnl");
        sb.Append(configuration.DisableBellOnQueueFull ? " -imaxbel" : " imaxbel");
        sb.Append(configuration.DisableXonXoffFlowControl ? " -ixon" : " ixon");

        // Output flags
        sb.Append(configuration.DisableOutputProcessing ? " -opost" : " opost");
        sb.Append(configuration.DisableMapNLtoCRNL ? " -onlcr" : " onlcr");

        // Local flags
        sb.Append(configuration.DisableSignalGeneration ? " -isig" : " isig");
        sb.Append(configuration.DisableCanonicalMode ? " -icanon" : " icanon");
        sb.Append(configuration.DisableExtendedProcessing ? " -iexten" : " iexten");
        sb.Append(configuration.DisableEcho ? " -echo" : " echo");
        sb.Append(configuration.DisableEchoErase ? " -echoe" : " echoe");
        sb.Append(configuration.DisableEchoKill ? " -echok" : " echok");
        sb.Append(configuration.DisableEchoControl ? " -echoctl" : " echoctl");
        sb.Append(configuration.DisableEchoKillErase ? " -echoke" : " echoke");

        // Control flags
        sb.Append(configuration.DisableHardwareFlowControl ? " -crtscts" : " crtscts");
        sb.Append(configuration.OddParity ? " parodd" : " -parodd");
        sb.Append(configuration.ParityEnabled ? " parenb" : " -parenb");

        // Special modes
        if (configuration.RawMode)
        {
            sb.Append(" raw");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates an stty command for a serial port profile.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="profile">The profile to generate the command for.</param>
    /// <returns>The stty command string.</returns>
    public string GenerateSttyCommandForProfile(string portPath, SerialPortProfile profile)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(profile, nameof(profile));

        return GenerateSttyCommand(portPath, profile.Configuration);
    }

    /// <summary>
    /// Generates stty arguments for a serial port configuration.
    /// </summary>
    /// <param name="portPath">The path to the port.</param>
    /// <param name="configuration">The configuration to generate the arguments for.</param>
    /// <returns>A list of stty arguments.</returns>
    public IEnumerable<string> GenerateSttyArguments(string portPath, SerialPortConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(portPath))
        {
            throw new ArgumentException("Port path cannot be null or empty", nameof(portPath));
        }

        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        var args = new List<string> { "-F", portPath };

        // Character size
        args.Add($"cs{configuration.CharacterSize}");

        // Baud rate
        args.Add(configuration.BaudRate.ToString());

        // Input flags
        args.Add(configuration.IgnoreBreak ? "ignbrk" : "-ignbrk");
        args.Add(configuration.DisableBreakInterrupt ? "-brkint" : "brkint");
        args.Add(configuration.DisableMapCRtoNL ? "-icrnl" : "icrnl");
        args.Add(configuration.DisableBellOnQueueFull ? "-imaxbel" : "imaxbel");
        args.Add(configuration.DisableXonXoffFlowControl ? "-ixon" : "ixon");

        // Output flags
        args.Add(configuration.DisableOutputProcessing ? "-opost" : "opost");
        args.Add(configuration.DisableMapNLtoCRNL ? "-onlcr" : "onlcr");

        // Local flags
        args.Add(configuration.DisableSignalGeneration ? "-isig" : "isig");
        args.Add(configuration.DisableCanonicalMode ? "-icanon" : "icanon");
        args.Add(configuration.DisableExtendedProcessing ? "-iexten" : "iexten");
        args.Add(configuration.DisableEcho ? "-echo" : "echo");
        args.Add(configuration.DisableEchoErase ? "-echoe" : "echoe");
        args.Add(configuration.DisableEchoKill ? "-echok" : "echok");
        args.Add(configuration.DisableEchoControl ? "-echoctl" : "echoctl");
        args.Add(configuration.DisableEchoKillErase ? "-echoke" : "echoke");

        // Control flags
        args.Add(configuration.DisableHardwareFlowControl ? "-crtscts" : "crtscts");
        args.Add(configuration.OddParity ? "parodd" : "-parodd");
        args.Add(configuration.ParityEnabled ? "parenb" : "-parenb");

        // Special modes
        if (configuration.RawMode)
        {
            args.Add("raw");
        }

        return args;
    }

    /// <summary>
    /// Executes an stty command.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The command execution result.</returns>
    [Obsolete("Use ExecuteSttyDirectAsync instead to avoid shell injection vulnerabilities.")]
    public async Task<SttyCommandResult> ExecuteSttyCommandAsync(
        string command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _shellExecutor.ExecuteCommandWithTimeoutAsync(command, 5000, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return new SttyCommandResult
            {
                Success = result.Success,
                ExitCode = result.ExitCode,
                StandardOutput = result.Output.Trim(),
                StandardError = result.Error.Trim(),
                ExecutionTime = stopwatch.Elapsed,
                Command = command
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to execute stty command: {Command}", command);

            return new SttyCommandResult
            {
                Success = false,
                ExitCode = -1,
                StandardOutput = "",
                StandardError = ex.Message,
                ExecutionTime = stopwatch.Elapsed,
                Command = command
            };
        }
    }

    /// <summary>
    /// Executes an stty command directly without a shell.
    /// </summary>
    /// <param name="arguments">The arguments for the stty command.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The command execution result.</returns>
    public async Task<SttyCommandResult> ExecuteSttyDirectAsync(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _shellExecutor.ExecuteDirectAsync("stty", arguments, 5000, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            return new SttyCommandResult
            {
                Success = result.Success,
                ExitCode = result.ExitCode,
                StandardOutput = result.Output.Trim(),
                StandardError = result.Error.Trim(),
                ExecutionTime = stopwatch.Elapsed,
                Command = $"stty {string.Join(" ", arguments)}"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to execute stty command: stty {Arguments}", string.Join(" ", arguments));

            return new SttyCommandResult
            {
                Success = false,
                ExitCode = -1,
                StandardOutput = "",
                StandardError = ex.Message,
                ExecutionTime = stopwatch.Elapsed,
                Command = $"stty {string.Join(" ", arguments)}"
            };
        }
    }

    /// <summary>
    /// Validates an stty command for security and correctness.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>The validation result.</returns>
    public SttyCommandValidationResult ValidateSttyCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var result = new SttyCommandValidationResult
        {
            ValidatedCommand = command.Trim()
        };

        // Basic validation
        if (!command.TrimStart().StartsWith("stty", StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add("Command must start with 'stty'");
            return result;
        }

        // Check for dangerous commands
        string[] dangerousPatterns =
        [
            @"`", @"\$", @"&&", @"\|\|", @";", @"\("
        ];

        foreach (string? pattern in dangerousPatterns)
        {
            if (new Regex(pattern).IsMatch(command))
            {
                result.Errors.Add($"Command contains potentially dangerous shell metacharacters: {pattern}");
            }
        }

        // Check for redirection to sensitive devices
        if (new Regex(@">\s*/dev/").IsMatch(command))
        {
            result.Errors.Add("Command contains potentially dangerous redirection to /dev/");
        }

        // Check for required -F flag
        if (!DeviceFlagRegex().IsMatch(command))
        {
            result.Warnings.Add("Command should specify a device with -F flag");
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    #region Private Methods

    /// <summary>
    /// Parses stty output to create a SerialPortConfiguration.
    /// </summary>
    /// <param name="sttyOutput">The output from stty command.</param>
    /// <returns>A SerialPortConfiguration parsed from the output.</returns>
    private SerialPortConfiguration ParseSttyOutput(string sttyOutput)
    {
        var config = new SerialPortConfiguration();

        try
        {
            // Parse baud rate
            Match baudMatch = BaudRateRegex().Match(sttyOutput);
            if (baudMatch.Success && int.TryParse(baudMatch.Groups[1].Value, out int baud))
            {
                config.BaudRate = baud;
            }

            // Parse character size
            Match csMatch = CharacterSizeRegex().Match(sttyOutput);
            if (csMatch.Success && int.TryParse(csMatch.Groups[1].Value, out int cs))
            {
                config.CharacterSize = cs;
            }

            // Parse flags (simplified parsing - would need more comprehensive implementation)
            config.ParityEnabled = sttyOutput.Contains("parenb");
            config.OddParity = sttyOutput.Contains("parodd") && !sttyOutput.Contains("-parodd");
            config.DisableHardwareFlowControl = sttyOutput.Contains("-crtscts");
            config.IgnoreBreak = sttyOutput.Contains("ignbrk");
            config.DisableBreakInterrupt = sttyOutput.Contains("-brkint");
            config.DisableMapCRtoNL = sttyOutput.Contains("-icrnl");
            config.DisableBellOnQueueFull = sttyOutput.Contains("-imaxbel");
            config.DisableXonXoffFlowControl = sttyOutput.Contains("-ixon");
            config.DisableOutputProcessing = sttyOutput.Contains("-opost");
            config.DisableMapNLtoCRNL = sttyOutput.Contains("-onlcr");
            config.DisableSignalGeneration = sttyOutput.Contains("-isig");
            config.DisableCanonicalMode = sttyOutput.Contains("-icanon");
            config.DisableExtendedProcessing = sttyOutput.Contains("-iexten");
            config.DisableEcho = sttyOutput.Contains("-echo");
            config.DisableEchoErase = sttyOutput.Contains("-echoe");
            config.DisableEchoKill = sttyOutput.Contains("-echok");
            config.DisableEchoControl = sttyOutput.Contains("-echoctl");
            config.DisableEchoKillErase = sttyOutput.Contains("-echoke");

            config.ModifiedAt = _timeProvider.GetLocalNow();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse stty output completely");
        }

        return config;
    }

    #endregion

    #region Regex Generation

    [GeneratedRegex(@"-F\s+/dev/tty", RegexOptions.IgnoreCase)]
    private static partial Regex DeviceFlagRegex();

    [GeneratedRegex(@"speed (\d+) baud")]
    private static partial Regex BaudRateRegex();

    [GeneratedRegex(@"cs(\d)")]
    private static partial Regex CharacterSizeRegex();

    private static Regex DangerousPatternRegex(string pattern) => new(pattern, RegexOptions.IgnoreCase);

    #endregion
}
