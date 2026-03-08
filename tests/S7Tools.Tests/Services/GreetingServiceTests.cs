using S7Tools.Core.Services.Interfaces;
using S7Tools.Services;
using S7Tools.Services.Interfaces;
using S7Tools.Resources.Strings;

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

        // Mocking ILocalizationService with explicit params matching to avoid fragility
        _localizationServiceMock.Setup(x => x.GetString(expectedKey, It.Is<object[]>(args => args.Length == 1 && (string)args[0] == name)))
                                .Returns($"Localized {expectedKey} {name}");

        // Act
        string result = _greetingService.Greet(name);

        // Assert
        result.Should().Be($"Localized {expectedKey} {name}");
        _localizationServiceMock.Verify(x => x.GetString(expectedKey, It.Is<object[]>(args => args.Length == 1 && (string)args[0] == name)), Times.Once);
    }

    [Fact]
    public void Greet_ShouldUseRealResources_WhenUsingIntegrationTestPattern()
    {
        // This test validates that the resource keys actually exist in the resx file
        // by checking the generated designer class properties
        UIStrings.Greeting_Morning.Should().NotBeNullOrEmpty();
        UIStrings.Greeting_Afternoon.Should().NotBeNullOrEmpty();
        UIStrings.Greeting_Evening.Should().NotBeNullOrEmpty();

        UIStrings.Greeting_Morning.Should().Contain("{0}");
        UIStrings.Greeting_Afternoon.Should().Contain("{0}");
        UIStrings.Greeting_Evening.Should().Contain("{0}");
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
