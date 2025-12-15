using System;
using Xunit;
using S7Tools.Services.Adapters.Plc;

namespace S7Tools.Tests.Services.Adapters;

/// <summary>
/// Unit tests for DumpProgressTracker to verify progress, speed, and ETA calculations.
/// </summary>
public class DumpProgressTrackerTests
{
    [Fact(DisplayName = "Constructor initializes tracker with valid total bytes")]
    public void Constructor_WithValidTotalBytes_InitializesCorrectly()
    {
        // Arrange & Act
        var tracker = new DumpProgressTracker(1000);

        // Assert
        Assert.Equal(1000, tracker.TotalBytes);
        Assert.Equal(0, tracker.BytesReceived);
        Assert.Equal(1000, tracker.BytesRemaining);
        Assert.Equal(0, tracker.ProgressPercentage);
    }

    [Fact(DisplayName = "Constructor throws ArgumentException with zero or negative bytes")]
    public void Constructor_WithZeroOrNegativeBytes_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new DumpProgressTracker(0));
        Assert.Throws<ArgumentException>(() => new DumpProgressTracker(-100));
    }

    [Fact(DisplayName = "Update calculates correct progress percentage")]
    public void Update_CalculatesCorrectProgressPercentage()
    {
        // Arrange
        var tracker = new DumpProgressTracker(1000);

        // Act
        tracker.Update(250);

        // Assert
        Assert.Equal(250, tracker.BytesReceived);
        Assert.Equal(750, tracker.BytesRemaining);
        Assert.Equal(25.0, tracker.ProgressPercentage);
    }

    [Fact(DisplayName = "Update throws ArgumentException when bytes decrease")]
    public void Update_WithDecreasingBytes_ThrowsArgumentException()
    {
        // Arrange
        var tracker = new DumpProgressTracker(1000);
        tracker.Update(500);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => tracker.Update(400));
    }

    [Fact(DisplayName = "FormatBytes returns correct human-readable format")]
    public void FormatBytes_ReturnsCorrectFormat()
    {
        // Act & Assert
        Assert.Equal("512 B", DumpProgressTracker.FormatBytes(512));
        Assert.Equal("1.00 KB", DumpProgressTracker.FormatBytes(1024));
        Assert.Equal("1.50 MB", DumpProgressTracker.FormatBytes(1024 * 1024 + 512 * 1024));
        Assert.Equal("2.00 GB", DumpProgressTracker.FormatBytes(2L * 1024 * 1024 * 1024));
    }

    [Fact(DisplayName = "FormatTimeSpan returns correct human-readable format")]
    public void FormatTimeSpan_ReturnsCorrectFormat()
    {
        // Act & Assert
        Assert.Equal("30s", DumpProgressTracker.FormatTimeSpan(TimeSpan.FromSeconds(30)));
        Assert.Equal("2m 30s", DumpProgressTracker.FormatTimeSpan(TimeSpan.FromSeconds(150)));
        Assert.Equal("1h 30m", DumpProgressTracker.FormatTimeSpan(TimeSpan.FromMinutes(90)));
    }

    [Fact(DisplayName = "Speed calculation returns zero initially")]
    public void SpeedBytesPerSecond_InitiallyReturnsZero()
    {
        // Arrange
        var tracker = new DumpProgressTracker(1000);

        // Act
        double speed = tracker.SpeedBytesPerSecond;

        // Assert
        Assert.Equal(0, speed);
    }

    [Fact(DisplayName = "EstimatedTimeRemaining returns null initially")]
    public void EstimatedTimeRemaining_InitiallyReturnsNull()
    {
        // Arrange
        var tracker = new DumpProgressTracker(1000);

        // Act
        TimeSpan? eta = tracker.EstimatedTimeRemaining;

        // Assert
        Assert.Null(eta);
    }

    [Fact(DisplayName = "EstimatedTimeRemaining returns zero when complete")]
    public void EstimatedTimeRemaining_WhenComplete_ReturnsZero()
    {
        // Arrange
        var tracker = new DumpProgressTracker(1000);
        
        // Simulate some progress to establish speed
        tracker.Update(500);
        System.Threading.Thread.Sleep(200); // Wait for speed calculation
        tracker.Update(1000); // Complete

        // Act
        TimeSpan? eta = tracker.EstimatedTimeRemaining;

        // Assert
        Assert.NotNull(eta);
        Assert.Equal(TimeSpan.Zero, eta.Value);
    }

    [Fact(DisplayName = "GetSummary returns formatted progress information")]
    public void GetSummary_ReturnsFormattedString()
    {
        // Arrange
        var tracker = new DumpProgressTracker(1024 * 1024); // 1 MB

        // Act
        tracker.Update(512 * 1024); // 512 KB
        string summary = tracker.GetSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.Contains("512.00 KB", summary);
        Assert.Contains("1.00 MB", summary);
        Assert.Contains("50.0%", summary);
    }
}
