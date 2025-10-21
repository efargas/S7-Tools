using S7Tools.ViewModels.Profiles;

namespace S7Tools.Services;

/// <summary>
/// Result of profile validation with detailed information for UI display
/// </summary>
public class ProfileDisplayValidationResult
{
    /// <summary>
    /// Overall validation state
    /// </summary>
    public ProfileValidationState State { get; init; }

    /// <summary>
    /// Validation messages (warnings, errors)
    /// </summary>
    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Property-specific validation results
    /// </summary>
    public IReadOnlyDictionary<string, PropertyValidationState> PropertyStates { get; init; }
        = new Dictionary<string, PropertyValidationState>();

    /// <summary>
    /// True if profile is valid (no errors)
    /// </summary>
    public bool IsValid => State != ProfileValidationState.Error;

    /// <summary>
    /// True if profile has warnings
    /// </summary>
    public bool HasWarnings => State == ProfileValidationState.Warning || Messages.Any();
}

/// <summary>
/// Overall profile validation state
/// </summary>
public enum ProfileValidationState
{
    Valid,
    Warning,
    Error,
    Missing
}
