using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

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
