namespace S7Tools.Core.Services.Interfaces;

/// <summary>
/// Provides an abstraction for retrieving the current date and time.
/// Used to enable testability and consistency across the application.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current date and time in Coordinated Universal Time (UTC).
    /// </summary>
    DateTime GetUtcNow();

    /// <summary>
    /// Gets the current date and time expressed as the local time.
    /// </summary>
    DateTime GetLocalNow();
}
