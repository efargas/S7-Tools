using Microsoft.Extensions.Logging;

namespace S7Tools.Services;

/// <summary>
/// Service for consistent error display across the application
/// </summary>
public interface IErrorDisplayService
{
    /// <summary>
    /// Creates a user-friendly error message for missing profiles
    /// </summary>
    /// <param name="profileId">Missing profile ID</param>
    /// <param name="profileType">Type of missing profile</param>
    /// <param name="lastKnownName">Last known profile name</param>
    /// <returns>Formatted error message</returns>
    string CreateMissingProfileMessage(Guid profileId, string profileType, string? lastKnownName = null);

    /// <summary>
    /// Creates an error message for corrupted profile data
    /// </summary>
    /// <param name="profileName">Profile name</param>
    /// <param name="profileType">Profile type</param>
    /// <param name="validationErrors">Specific validation errors</param>
    /// <returns>Formatted error message</returns>
    string CreateCorruptedProfileMessage(string profileName, string profileType, IEnumerable<string> validationErrors);

    /// <summary>
    /// Creates an error message for service unavailable scenarios
    /// </summary>
    /// <param name="serviceName">Name of unavailable service</param>
    /// <param name="operation">Operation that failed</param>
    /// <returns>Formatted error message</returns>
    string CreateServiceUnavailableMessage(string serviceName, string operation);
}

/// <summary>
/// Implementation of error display service for consistent error messaging
/// </summary>
public class ErrorDisplayService : IErrorDisplayService
{
    private readonly ILogger<ErrorDisplayService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDisplayService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ErrorDisplayService(ILogger<ErrorDisplayService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string CreateMissingProfileMessage(Guid profileId, string profileType, string? lastKnownName = null)
    {
        _logger.LogWarning("Creating missing profile message for {ProfileType} {ProfileId} (last known name: {LastKnownName})",
            profileType, profileId, lastKnownName);

        string baseMessage = $"Profile not found: {profileType}";

        if (!string.IsNullOrWhiteSpace(lastKnownName))
        {
            baseMessage += $" '{lastKnownName}'";
        }

        baseMessage += $" (ID: {profileId:D})";
        baseMessage += ". The profile may have been deleted or moved.";

        return baseMessage;
    }

    /// <inheritdoc />
    public string CreateCorruptedProfileMessage(string profileName, string profileType, IEnumerable<string> validationErrors)
    {
        List<string> errors = validationErrors?.ToList() ?? new List<string>();

        _logger.LogWarning("Creating corrupted profile message for {ProfileType} '{ProfileName}' with {ErrorCount} errors",
            profileType, profileName, errors.Count);

        string baseMessage = $"Profile data corrupted: {profileType} '{profileName}'";

        if (errors.Count != 0)
        {
            baseMessage += $". Issues: {string.Join(", ", errors)}";
        }
        else
        {
            baseMessage += ". The profile data appears to be invalid or incomplete.";
        }

        return baseMessage;
    }

    /// <inheritdoc />
    public string CreateServiceUnavailableMessage(string serviceName, string operation)
    {
        _logger.LogError("Service unavailable: {ServiceName} for operation {Operation}", serviceName, operation);

        return $"Service temporarily unavailable: {serviceName} for {operation}. Please try again later or contact support if the problem persists.";
    }
}
