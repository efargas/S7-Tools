using S7Tools.Core.Services.Interfaces;
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
            >= MorningStartHour and < AfternoonStartHour => MorningGreetingKey,
            >= AfternoonStartHour and < EveningStartHour => AfternoonGreetingKey,
            _ => EveningGreetingKey
        };

        return _localizationService.GetString(greetingKey, name);
    }
}
