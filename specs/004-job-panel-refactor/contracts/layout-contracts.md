# Layout Contracts: Job Panel Layout Refactor

**Feature**: Job Panel Layout Refactor
**Date**: 2025-10-21
**Context**: Interface contracts for responsive layout behavior

## Overview

This feature modifies existing UI layout behavior rather than creating new API contracts. The contracts defined here describe the expected interface behavior for layout components and their interactions.

## Grid Layout Contract

### MainGrid Layout States

```csharp
public enum PanelState
{
    Collapsed,  // Activity bar visible, panel hidden
    Expanded,   // Panel visible, activity bar hidden
    Resizing    // Intermediate state during drag operations
}
```

### Column Definition Contract

```csharp
public interface IGridLayoutManager
{
    // Column width configuration for different states
    GridLength[] GetColumnWidths(PanelState state, double panelWidth = 400);

    // State transition management
    void TransitionToState(PanelState newState, double? targetPanelWidth = null);

    // Real-time resize support
    void UpdatePanelWidth(double newWidth);

    // Constraint validation
    bool IsValidPanelWidth(double width);
}
```

**Expected Column Configurations**:
- **Collapsed**: [*, 48px, 0, 0]
- **Expanded**: [*, 0, Auto, 400px] (or user-defined width)
- **Resizing**: [*, 0, Auto, 300px-600px] (clamped to valid range)

## Scroll Behavior Contract

### Horizontal Scrolling Interface

```csharp
public interface IScrollableContent
{
    // Automatic scrollbar management
    bool IsHorizontalScrollRequired { get; }

    // Content measurement
    double ContentWidth { get; }
    double AvailableWidth { get; }

    // Scroll position preservation
    Point ScrollPosition { get; set; }

    // Performance monitoring
    double ScrollPerformanceFps { get; }
}
```

**Expected Behavior**:
- Horizontal scrollbar appears when `ContentWidth > AvailableWidth`
- Scrollbar visibility updates automatically when available width changes
- Scroll position preserved during panel resize operations
- Minimum 60fps performance during scroll operations

## Panel Resize Contract

### GridSplitter Interface

```csharp
public interface IPanelResizer
{
    // Width constraints
    double MinWidth { get; } // 300px
    double MaxWidth { get; } // 600px
    double CurrentWidth { get; set; }

    // Resize events
    event EventHandler<double> WidthChanging;  // Real-time during drag
    event EventHandler<double> WidthChanged;   // Final width after drag

    // Constraint enforcement
    double ClampWidth(double requestedWidth);

    // Visual feedback
    bool IsResizing { get; }
}
```

**Expected Constraints**:
- Panel width must be between 300px and 600px
- Width changes must trigger immediate layout updates
- Real-time visual feedback during resize operations

## Window Edge Anchoring Contract

### Edge Alignment Interface

```csharp
public interface IWindowEdgeAnchored
{
    // Edge alignment properties
    bool IsAnchoredToRightEdge { get; }
    double GapFromWindowEdge { get; } // Must be 0

    // Window resize handling
    void OnWindowSizeChanged(Size newWindowSize);

    // Validation
    bool IsProperlyAnchored();
}
```

**Expected Behavior**:
- Panel right edge aligns perfectly with window right edge
- Zero pixel gap maintained at all times
- Edge alignment preserved during window resize operations

## Layout Performance Contract

### Performance Requirements

```csharp
public interface ILayoutPerformance
{
    // Timing requirements
    TimeSpan LayoutUpdateTime { get; }     // Must be ≤ 16ms (60fps)
    TimeSpan ResizeResponseTime { get; }   // Must be ≤ 16ms

    // Smoothness metrics
    bool IsLayoutUpdateSmooth { get; }
    int DroppedFrameCount { get; }

    // Performance monitoring
    void MeasureLayoutPerformance();
    PerformanceMetrics GetPerformanceMetrics();
}
```

**Performance Guarantees**:
- Layout updates complete within 16ms (60fps target)
- No visual glitches during rapid window resizing
- Smooth real-time feedback during panel resize operations

## Event Contracts

### Layout Event Interface

```csharp
public interface ILayoutEvents
{
    // Panel state events
    event EventHandler<PanelState> PanelStateChanged;
    event EventHandler<double> PanelWidthChanged;

    // Layout update events
    event EventHandler LayoutUpdateStarted;
    event EventHandler LayoutUpdateCompleted;

    // Error events
    event EventHandler<string> LayoutError;
}
```

## Implementation Notes

### Code-Behind Contract

The existing `JobsMainContentView.axaml.cs` file implements these contracts through:

- **OnJobInfoToggleClick**: Handles collapsed→expanded transition
- **OnCloseJobInfoPanelClick**: Handles expanded→collapsed transition
- **Grid column management**: Dynamic GridLength updates for responsive behavior

### XAML Binding Contract

The `JobsMainContentView.axaml` file provides:

- **Grid structure**: 4-column layout with proper column definitions
- **GridSplitter**: ResizeDirection="Columns" with constraint enforcement
- **ScrollViewer**: HorizontalScrollBarVisibility="Auto" for main content
- **Panel anchoring**: Border elements with zero margins for edge alignment

These contracts ensure consistent, predictable layout behavior that meets all functional and performance requirements.
