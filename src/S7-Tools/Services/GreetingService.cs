using S7_Tools.Services.Interfaces;

namespace S7_Tools.Services;

public class GreetingService : IGreetingService
{
    public string Greet(string name)
    {
        return $"Hello, {name}!";
    }
}