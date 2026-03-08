using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// A service that provides greeting messages.
/// </summary>
public class GreetingService : IGreetingService
{
    private readonly ITimeProvider _timeProvider;
    private readonly ILocalizationService _localizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GreetingService"/> class.
    /// </summary>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="localizationService">The localization service.</param>
    public GreetingService(ITimeProvider timeProvider, ILocalizationService localizationService)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
    }

    /// <inheritdoc/>
    public string Greet(string name)
    {
        DateTime now = _timeProvider.GetLocalNow();
        string greetingKey = now.Hour switch
        {
            >= 5 and < 12 => "Greeting_Morning",
            >= 12 and < 18 => "Greeting_Afternoon",
            _ => "Greeting_Evening"
        };

        return _localizationService.GetString(greetingKey, name);
    }
}
