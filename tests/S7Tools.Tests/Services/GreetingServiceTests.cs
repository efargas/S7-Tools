using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using S7Tools.Services.Interfaces;

namespace S7Tools.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="GreetingService"/> class.
/// </summary>
public class GreetingServiceTests
{
    private const string MorningGreetingKey = "Greeting_Morning";
    private const string AfternoonGreetingKey = "Greeting_Afternoon";
    private const string EveningGreetingKey = "Greeting_Evening";

    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GreetingService _greetingService;

    public GreetingServiceTests()
    {
        _timeProviderMock = new Mock<ITimeProvider>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _greetingService = new GreetingService(_timeProviderMock.Object, _localizationServiceMock.Object);
    }

    [Theory]
    [InlineData(5, MorningGreetingKey)]
    [InlineData(8, MorningGreetingKey)]
    [InlineData(11, MorningGreetingKey)]
    [InlineData(12, AfternoonGreetingKey)]
    [InlineData(15, AfternoonGreetingKey)]
    [InlineData(17, AfternoonGreetingKey)]
    [InlineData(18, EveningGreetingKey)]
    [InlineData(21, EveningGreetingKey)]
    [InlineData(0, EveningGreetingKey)]
    [InlineData(4, EveningGreetingKey)]
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GreetingService _greetingService;

    public GreetingServiceTests()
    {
        _timeProviderMock = new Mock<ITimeProvider>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _greetingService = new GreetingService(_timeProviderMock.Object, _localizationServiceMock.Object);
    }

    [Theory]
    [InlineData(5, GreetingService.MorningGreetingKey)]
    [InlineData(8, GreetingService.MorningGreetingKey)]
    [InlineData(11, GreetingService.MorningGreetingKey)]
    [InlineData(12, GreetingService.AfternoonGreetingKey)]
    [InlineData(15, GreetingService.AfternoonGreetingKey)]
    [InlineData(17, GreetingService.AfternoonGreetingKey)]
    [InlineData(18, GreetingService.EveningGreetingKey)]
    [InlineData(21, GreetingService.EveningGreetingKey)]
    [InlineData(0, GreetingService.EveningGreetingKey)]
    [InlineData(4, GreetingService.EveningGreetingKey)]
    public void Greet_ShouldReturnLocalizedGreeting_BasedOnTimeOfDay(int hour, string expectedKey)
    {
        // Arrange
        string name = "User";
        DateTime testTime = new DateTime(2023, 1, 1, hour, 0, 0);
        _timeProviderMock.Setup(x => x.GetLocalNow()).Returns(testTime);
        _localizationServiceMock.Setup(x => x.GetString(expectedKey, name))
                                .Returns($"Localized {expectedKey} {name}");

        // Act
        string result = _greetingService.Greet(name);

        // Assert
        result.Should().Be($"Localized {expectedKey} {name}");
        _localizationServiceMock.Verify(x => x.GetString(expectedKey, name), Times.Once);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenTimeProviderIsNull()
    {
        // Act
        Action act = () => new GreetingService(null!, _localizationServiceMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLocalizationServiceIsNull()
    {
        // Act
        Action act = () => new GreetingService(_timeProviderMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("localizationService");
    }
}
