using S7_Tools.Services.Interfaces;

namespace S7_Tools.Services;

/// <summary>
/// A service that provides greeting messages.
/// </summary>
public class GreetingService : IGreetingService
{
    /// <inheritdoc/>
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}