using S7Tools.Core.Services.Interfaces;
using S7Tools.Resources.Strings;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// A service that provides greeting messages.
/// </summary>
/// <param name="timeProvider">The time provider.</param>
/// <param name="localizationService">The localization service.</param>
public sealed class GreetingService(ITimeProvider timeProvider, ILocalizationService localizationService) : IGreetingService
{
    private readonly ITimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly ILocalizationService _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

    /// <inheritdoc/>
    public string Greet(string name)
    {
        DateTime now = _timeProvider.GetLocalNow();
        string greetingKey = now.Hour switch
        {
            >= 5 and < 12 => nameof(UIStrings.Greeting_Morning),
            >= 12 and < 18 => nameof(UIStrings.Greeting_Afternoon),
            _ => nameof(UIStrings.Greeting_Evening)
        };

        return _localizationService.GetString(greetingKey, name);
    }
}
