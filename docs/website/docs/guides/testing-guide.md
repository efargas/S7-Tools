---
title: "Testing Guide"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "testing", "quality", "tdd"]
related:
  - docs/guides/development-workflow.md
  - docs/architecture/clean-architecture.md
  - docs/patterns/_index.md
---

# Testing Guide

This guide covers testing standards, practices, and patterns for S7Tools following Test-Driven Development (TDD) principles and constitutional requirements.

## Quality Baseline

**Current Standards** (as of 2025-11-10):
- **Test Count**: 361 tests (360 passing, 1 intentionally skipped)
- **Pass Rate**: 99.7%+ (constitutional requirement)
- **Code Coverage**: Target 80%+ for critical paths
- **Build**: 0 errors, 0 warnings

## Test-First Principle (NON-NEGOTIABLE)

**Constitutional Requirement** (Article III):
> All code changes MUST include tests. Tests are written BEFORE implementation (Test-Driven Development).

### TDD Cycle

```
1. RED: Write failing test
   ↓
2. GREEN: Write minimal code to pass
   ↓
3. REFACTOR: Improve code while keeping tests green
   ↓
Repeat
```

## Test Organization

### Project Structure

```
tests/
├── S7Tools.Tests/                    # UI/Application layer tests
│   ├── ViewModels/                   # ViewModel tests (by category)
│   ├── Services/                     # Service tests
│   └── Controls/                     # Control tests
├── S7Tools.Core.Tests/               # Core/Domain layer tests
│   ├── Models/                       # Model tests
│   ├── Services/                     # Service interface tests
│   └── Exceptions/                   # Exception tests
└── S7Tools.Infrastructure.Logging.Tests/  # Infrastructure tests
    └── Providers/                    # Provider tests
```

### File Naming Convention

```
SourceFile.cs → SourceFileTests.cs

Examples:
SerialProfileService.cs → SerialProfileServiceTests.cs
ProfileManagementViewModel.cs → ProfileManagementViewModelTests.cs
```

## AAA Pattern (Arrange-Act-Assert)

**Standard Structure** for all tests:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var mockService = Substitute.For<IService>();
    mockService.GetDataAsync().Returns(Task.FromResult(testData));
    var sut = new SystemUnderTest(mockService);

    // Act - Execute the method being tested
    var result = await sut.MethodAsync(input);

    // Assert - Verify expected outcome
    Assert.NotNull(result);
    Assert.Equal(expectedValue, result.Property);
}
```

### Naming Convention

```
MethodName_Scenario_ExpectedBehavior

Examples:
- CreateProfileAsync_ValidData_ReturnsNewProfile
- DeleteProfileAsync_NonExistentId_ThrowsProfileNotFoundException
- LoadProfilesAsync_EmptyFile_ReturnsEmptyCollection
- UpdateProfileAsync_ConcurrentUpdate_ThrowsConcurrencyException
```

## Test Frameworks

### Primary Framework: xUnit

```csharp
using Xunit;

public class MyServiceTests
{
    [Fact]  // Single test case
    public void Method_Scenario_Behavior()
    {
        // ...
    }

    [Theory]  // Multiple test cases with same logic
    [InlineData(1, "expected1")]
    [InlineData(2, "expected2")]
    public void Method_MultipleScenarios_Behavior(int input, string expected)
    {
        // ...
    }
}
```

### Mocking: NSubstitute

```csharp
using NSubstitute;

// Create mock
var mockService = Substitute.For<IProfileService>();

// Setup return value
mockService.GetProfileAsync(Arg.Any<Guid>())
    .Returns(Task.FromResult(testProfile));

// Verify call
await mockService.Received(1).GetProfileAsync(profileId);

// Verify no call
mockService.DidNotReceive().DeleteProfileAsync(Arg.Any<Guid>());
```

### Assertions: FluentAssertions

```csharp
using FluentAssertions;

// Collection assertions
result.Should().NotBeNull();
result.Should().HaveCount(3);
result.Should().Contain(x => x.Id == expectedId);

// Exception assertions
var act = () => sut.ThrowingMethod();
await act.Should().ThrowAsync<ProfileNotFoundException>()
    .WithMessage("*not found*");

// Object assertions
result.Should().BeEquivalentTo(expected, options =>
    options.Excluding(x => x.CreatedDate));
```

## Testing Patterns

### Pattern: Testing ViewModels

```csharp
public class ProfileManagementViewModelTests
{
    private readonly IProfileService _mockService;
    private readonly ProfileManagementViewModel _sut;

    public ProfileManagementViewModelTests()
    {
        _mockService = Substitute.For<IProfileService>();
        _sut = new ProfileManagementViewModel(_mockService);
    }

    [Fact]
    public async Task LoadProfilesCommand_ValidData_PopulatesCollection()
    {
        // Arrange
        var profiles = new List<SerialProfile>
        {
            new() { Id = Guid.NewGuid(), Name = "Profile1" }
        };
        _mockService.GetAllAsync().Returns(Task.FromResult<IEnumerable<SerialProfile>>(profiles));

        // Act
        await _sut.LoadProfilesCommand.Execute();

        // Assert
        _sut.Profiles.Should().HaveCount(1);
        _sut.Profiles[0].Name.Should().Be("Profile1");
    }

    [Fact]
    public async Task DeleteCommand_ConfirmedDeletion_RemovesProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        _sut.SelectedProfile = new SerialProfile { Id = profileId };
        _mockService.DeleteAsync(profileId).Returns(Task.FromResult(true));

        // Act
        await _sut.DeleteCommand.Execute();

        // Assert
        await _mockService.Received(1).DeleteAsync(profileId);
    }
}
```

### Pattern: Testing Services

```csharp
public class SerialProfileServiceTests
{
    private readonly ILogger<SerialProfileService> _mockLogger;
    private readonly SerialProfileService _sut;

    public SerialProfileServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<SerialProfileService>>();
        _sut = new SerialProfileService(_mockLogger);
    }

    [Fact]
    public async Task CreateAsync_ValidProfile_ReturnsNewProfile()
    {
        // Arrange
        var profile = new SerialProfile { Name = "Test" };

        // Act
        var result = await _sut.CreateAsync(profile);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ThrowsException()
    {
        // Arrange
        await _sut.CreateAsync(new SerialProfile { Name = "Duplicate" });

        // Act
        var act = () => _sut.CreateAsync(new SerialProfile { Name = "Duplicate" });

        // Assert
        await act.Should().ThrowAsync<DuplicateProfileNameException>();
    }
}
```

### Pattern: Testing Async Methods

```csharp
[Fact]
public async Task AsyncMethod_Scenario_Behavior()
{
    // Arrange
    var mockService = Substitute.For<IAsyncService>();
    mockService.ExecuteAsync().Returns(Task.FromResult(expected));

    // Act
    var result = await sut.MethodAsync();

    // Assert - ALWAYS use await
    result.Should().Be(expected);

    // ❌ DON'T: Never use .Result or .Wait()
    // var result = sut.MethodAsync().Result;  // DEADLOCK RISK!
}
```

### Pattern: Testing Exceptions

```csharp
[Fact]
public async Task Method_InvalidInput_ThrowsSpecificException()
{
    // Arrange
    var invalidId = Guid.Empty;

    // Act
    var act = () => _sut.GetProfileAsync(invalidId);

    // Assert
    await act.Should().ThrowAsync<ProfileNotFoundException>()
        .WithMessage("*not found*")
        .Where(ex => ex.ProfileId == invalidId);
}

[Fact]
public void Method_NullArgument_ThrowsArgumentNullException()
{
    // Act
    var act = () => new Service(null!);

    // Assert
    act.Should().Throw<ArgumentNullException>()
        .WithParameterName("dependency");
}
```

### Pattern: Testing Thread Safety

```csharp
[Fact]
public async Task ConcurrentAccess_MultipleTasks_ThreadSafe()
{
    // Arrange
    var tasks = new List<Task>();
    var profileIds = Enumerable.Range(0, 10)
        .Select(_ => Guid.NewGuid())
        .ToList();

    // Act - Execute operations concurrently
    foreach (var id in profileIds)
    {
        tasks.Add(_sut.CreateProfileAsync(new SerialProfile { Id = id }));
    }
    await Task.WhenAll(tasks);

    // Assert - No exceptions, all profiles created
    var profiles = await _sut.GetAllAsync();
    profiles.Should().HaveCount(10);
}

[Fact]
public async Task SemaphoreProtectedMethod_NoConcurrentCalls_NoDeadlock()
{
    // Arrange
    var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

    // Act - Should complete within timeout
    var task1 = _sut.ProtectedMethodAsync();
    var task2 = _sut.ProtectedMethodAsync();

    // Assert - No deadlock
    await Task.WhenAll(task1, task2).WaitAsync(cancellationToken);
}
```

## Running Tests

### Command Line

```bash
# Run all tests
dotnet test src/S7Tools.sln --configuration Debug

# Run specific test project
dotnet test tests/S7Tools.Core.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~SerialProfileServiceTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~CreateAsync_ValidProfile_ReturnsNewProfile"

# Run with detailed output
dotnet test -v detailed

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Expected Output

```
Test run for S7Tools.Tests.dll (.NET 10.0)
Total tests: 361
     Passed: 360
    Skipped: 1
     Failed: 0

Total time: 2.5 Seconds
```

## Test Categories

### Unit Tests

**Purpose**: Test individual components in isolation

**Characteristics**:
- Fast (< 100ms per test)
- No external dependencies (use mocks)
- Deterministic (same input → same output)
- Independent (can run in any order)

**Example**:
```csharp
[Fact]
public void Profile_SetName_UpdatesProperty()
{
    // Pure unit test - no dependencies
    var profile = new SerialProfile();
    profile.Name = "Test";
    Assert.Equal("Test", profile.Name);
}
```

### Integration Tests

**Purpose**: Test interactions between components

**Characteristics**:
- Slower than unit tests
- May use real dependencies
- Test realistic scenarios

**Example**:
```csharp
[Fact]
public async Task ServiceChain_EndToEnd_WorksCorrectly()
{
    // Integration test - real services
    var service1 = new RealService1();
    var service2 = new RealService2(service1);
    var result = await service2.ProcessAsync(input);
    Assert.NotNull(result);
}
```

## Test Data Builders

**Pattern**: Create reusable test data builders

```csharp
public class SerialProfileBuilder
{
    private string _name = "Default";
    private int _baudRate = 9600;

    public SerialProfileBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public SerialProfileBuilder WithBaudRate(int rate)
    {
        _baudRate = rate;
        return this;
    }

    public SerialProfile Build()
    {
        return new SerialProfile
        {
            Id = Guid.NewGuid(),
            Name = _name,
            BaudRate = _baudRate
        };
    }
}

// Usage
var profile = new SerialProfileBuilder()
    .WithName("TestProfile")
    .WithBaudRate(115200)
    .Build();
```

## Common Testing Scenarios

### Scenario: Test Property Change Notifications

```csharp
[Fact]
public void Property_Changed_RaisesPropertyChanged()
{
    // Arrange
    var vm = new MyViewModel();
    var eventRaised = false;
    vm.PropertyChanged += (s, e) =>
    {
        if (e.PropertyName == nameof(vm.MyProperty))
            eventRaised = true;
    };

    // Act
    vm.MyProperty = "NewValue";

    // Assert
    eventRaised.Should().BeTrue();
}
```

### Scenario: Test Command CanExecute

```csharp
[Fact]
public void DeleteCommand_NoSelection_CannotExecute()
{
    // Arrange
    var vm = new MyViewModel();
    vm.SelectedItem = null;

    // Act & Assert
    vm.DeleteCommand.CanExecute(null).Should().BeFalse();
}

[Fact]
public void DeleteCommand_HasSelection_CanExecute()
{
    // Arrange
    var vm = new MyViewModel();
    vm.SelectedItem = new Item();

    // Act & Assert
    vm.DeleteCommand.CanExecute(null).Should().BeTrue();
}
```

## Troubleshooting Tests

### Flaky Tests

**Problem**: Test passes/fails randomly

**Solutions**:
- Remove time dependencies (use fake clocks)
- Avoid Thread.Sleep (use async/await properly)
- Mock external dependencies
- Use deterministic data

### Slow Tests

**Problem**: Test suite takes too long

**Solutions**:
- Profile tests to find slow ones
- Reduce setup/teardown overhead
- Use parallel execution where safe
- Mock expensive operations

### Test Isolation Issues

**Problem**: Tests affect each other

**Solutions**:
- Use fresh instances in each test
- Clear static state
- Use constructor for setup, not shared fields
- Avoid shared test data

## Best Practices

### DO

✅ Write tests before implementation (TDD)
✅ Use AAA pattern consistently
✅ Name tests descriptively
✅ Keep tests simple and focused
✅ Use builders for complex test data
✅ Mock external dependencies
✅ Test edge cases and error conditions
✅ Maintain 99.7%+ pass rate

### DON'T

❌ Use .Result or .Wait() in async tests
❌ Test implementation details
❌ Create test dependencies
❌ Ignore failing tests
❌ Skip writing tests (constitutional violation!)
❌ Use Thread.Sleep
❌ Share mutable state between tests
❌ Write tests that depend on execution order

## References

- [Development Workflow](./development-workflow.md)
- [Clean Architecture](../architecture/clean-architecture.md)
- [Pattern Catalog](../patterns/_index.md)
- [Latest Review](../reviews/LATEST.md)

---

*Last Updated*: 2025-11-10

## Related Documentation

- [Index](../INDEX.md)
- [Clean Architecture](../architecture/clean-architecture.md)
- [_Index](_index.md)
- [Code Style](code-style.md)
- [Development Workflow](development-workflow.md)
- [Onboarding](onboarding.md)
- [_Index](../patterns/_index.md)
- [Resource Coordinator Example](../patterns/examples/resource-coordinator-example.md)
- [2025 11 10 Quality Improvements](../reviews/2025-11-10-quality-improvements.md)
- [Latest](../reviews/LATEST.md)
- [_Index](../templates/_index.md)
- [Test Template](../templates/test-template.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2026-01-17*
