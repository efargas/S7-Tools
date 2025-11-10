---
title: "Test Template"
created: "2025-11-10"
last-updated: "2025-11-10"
version: "1.0.0"
status: "current"
tags:
  - template
  - code
  - testing
  - xunit
related:
  - docs/guides/testing-guide.md
  - docs/patterns/testing-patterns.md
---

# Test Template

Template for creating xUnit tests following AAA pattern (Arrange-Act-Assert).

## Usage

1. Copy code to `tests/S7Tools.Tests/[Feature]Tests.cs`
2. Replace `[FEATURE]` with feature under test
3. Follow AAA pattern for all test methods
4. Use async/await, never `.Result` or `.Wait()`

## Template Code

```csharp
using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using S7Tools.Services;
using S7Tools.Core.Interfaces;

namespace S7Tools.Tests;

public class [FEATURE]Tests : IDisposable
{
    private readonly Mock<ILoggingService> _mockLoggingService;
    private readonly [FEATURE]Service _sut; // System Under Test

    public [FEATURE]Tests()
    {
        // Arrange: Setup mocks and dependencies
        _mockLoggingService = new Mock<ILoggingService>();
        _sut = new [FEATURE]Service(_mockLoggingService.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCalled_ReturnsTrue()
    {
        // Arrange
        // (Additional setup if needed)

        // Act
        var result = await _sut.ExecuteAsync();

        // Assert
        result.Should().BeTrue();
        _mockLoggingService.Verify(
            x => x.LogInformation(It.IsAny<string>()),
            Times.AtLeastOnce
        );
    }

    [Theory]
    [InlineData("value1")]
    [InlineData("value2")]
    public async Task ExecuteAsync_WithDifferentInputs_ReturnsExpected(string input)
    {
        // Arrange
        // Setup for theory

        // Act
        var result = await _sut.ExecuteAsync();

        // Assert
        result.Should().NotBeNull();
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }
}
```

## Best Practices

- Use AAA pattern consistently
- One assertion per test preferred
- Test exception scenarios with `FluentAssertions.Async`
- Maintain 99.7%+ pass rate

## See Also

- ViewModel Template - `viewmodel-template.md`
- Service Template - `service-template.md`
- [Testing Guide](../guides/testing-guide.md)
