using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using S7Tools.Services.Interfaces;

namespace S7Tools.Tests.Services;

/// <summary>
/// Unit tests for the <see cref="GreetingService"/> class.
/// </summary>
public class GreetingServiceTests
{
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
    [InlineData(5, "Greeting_Morning")]
    [InlineData(8, "Greeting_Morning")]
    [InlineData(11, "Greeting_Morning")]
    [InlineData(12, "Greeting_Afternoon")]
    [InlineData(15, "Greeting_Afternoon")]
    [InlineData(17, "Greeting_Afternoon")]
    [InlineData(18, "Greeting_Evening")]
    [InlineData(21, "Greeting_Evening")]
    [InlineData(0, "Greeting_Evening")]
    [InlineData(4, "Greeting_Evening")]
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
