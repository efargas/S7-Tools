# Data Model: Job Panel Layout Refactor

**Feature**: Job Panel Layout Refactor
**Date**: 2025-10-21
**Context**: UI layout entities and their relationships for responsive panel behavior

## Overview

This feature involves refactoring the layout behavior of existing UI entities rather than creating new business data models. The focus is on how UI layout components interact and respond to user actions.

## UI Layout Entities

### MainGrid

**Purpose**: 4-column grid layout that manages the spatial arrangement of all main content areas

**Properties**:
- Column 0: Main content area (star sizing - claims all available space)
- Column 1: Activity bar (48px when collapsed, 0px when expanded)
- Column 2: GridSplitter (0px when collapsed, Auto when expanded)
- Column 3: Right panel (0px when collapsed, 300-600px when expanded)

**State Transitions**:
- Collapsed → Expanded: [*, 48, 0, 0] → [*, 0, Auto, 400]
- Expanded → Collapsed: [*, 0, Auto, NNNpx] → [*, 48, 0, 0]
- Resizing: [*, 0, Auto, 300-600px] (dynamic during drag operations)

**Validation Rules**:
- Panel width must be between 300px and 600px when expanded
- Main content area must always have star sizing to claim remaining space
- Activity bar and splitter visibility must be mutually exclusive

### JobProfilesDataGrid

**Purpose**: Primary content area that requires horizontal scrolling when constrained

**Properties**:
- Parent container: ScrollViewer with HorizontalScrollBarVisibility="Auto"
- Content width: Variable based on column count and data
- Available width: Calculated as window width minus panel width and margins

**Behavior**:
- When available width < content width: horizontal scrollbar appears
- When available width >= content width: no horizontal scrollbar
- Scrollbar visibility updates automatically when panel resizes

**Performance Requirements**:
- Horizontal scrolling must maintain 60fps performance
- Scroll position must be preserved during panel resize operations

### GridSplitter

**Purpose**: Interactive control for resizing the right panel width

**Properties**:
- Width: 4px
- ResizeDirection: Columns
- Background: #464647 (consistent with application theme)
- Cursor: SizeWestEast

**Constraints**:
- MinWidth enforcement: Prevents panel from becoming smaller than 300px
- MaxWidth enforcement: Prevents panel from exceeding 600px
- Real-time feedback: Visual updates during drag operations

**State Management**:
- Visible: Only when right panel is expanded
- Invisible: When right panel is collapsed (IsVisible=False)

### RightPanel

**Purpose**: Resizable information panel anchored to window edge

**Properties**:
- MinWidth: 300px
- MaxWidth: 600px
- DefaultWidth: 400px (when first expanded)
- Background: #252526
- BorderBrush: #464647
- BorderThickness: 1,0,0,0 (left border only)

**Edge Anchoring**:
- Right edge: Must align perfectly with window right edge (zero gap)
- Margins: 0 on all sides
- Padding: Defined by inner content, not the panel container

**Validation Rules**:
- Width must always be within 300-600px range
- Must extend fully to window edge without gaps
- Must maintain edge alignment during window resize

## Relationships

### MainGrid ↔ All Components
- **MainGrid** acts as the parent container managing spatial relationships
- Column definitions control available space for each child component
- Dynamic column width changes trigger layout updates for all children

### MainGrid ↔ JobProfilesDataGrid
- **MainGrid** Column 0 provides available width to content area
- **JobProfilesDataGrid** calculates horizontal scrolling based on available width
- Changes to panel width directly affect main content available space

### GridSplitter ↔ RightPanel
- **GridSplitter** controls **RightPanel** width through resize operations
- **RightPanel** width constraints are enforced by **GridSplitter** behavior
- Real-time feedback during resize operations updates both components

### Window ↔ MainGrid
- Window resize events trigger **MainGrid** layout recalculation
- **MainGrid** maintains responsive behavior across different window sizes
- Minimum window width (800px) ensures usable interface

## Layout Calculation Logic

### Available Space Calculation
```
MainContentWidth = WindowWidth - ActivityBarWidth - SplitterWidth - PanelWidth - Margins
```

### Horizontal Scrolling Decision
```
ShowHorizontalScrollbar = (DataGridContentWidth > MainContentWidth)
```

### Panel Width Constraints
```
PanelWidth = Clamp(UserRequestedWidth, MinWidth=300px, MaxWidth=600px)
```

## State Persistence

**Note**: This feature does not require persistent state storage. All layout state is maintained in-memory during the application session and resets to default values on application restart.

**Session State**:
- Panel expanded/collapsed state
- Panel width (when expanded)
- Main content scroll position

**Default Values**:
- Panel state: Collapsed
- Panel width: 400px (when first expanded)
- Scroll position: Top-left (0,0)

This data model provides the foundation for implementing responsive, constraint-aware layout behavior that meets all functional requirements.
