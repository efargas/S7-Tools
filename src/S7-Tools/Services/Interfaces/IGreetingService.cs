namespace S7_Tools.Services.Interfaces;

/// <summary>
/// Defines the contract for a service that provides greeting messages.
/// </summary>
public interface IGreetingService
{
    /// <summary>
    /// Generates a greeting message.
    /// </summary>
    /// <param name="name">The name to include in the greeting.</param>
    /// <returns>A greeting message.</returns>
    string Greet(string name);
}