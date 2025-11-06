# Port Discovery Control Refactor - Proposal

## Problem Statement

The serial port discovery functionality is currently duplicated across three views:
1. **SerialPortScannerView** - Standalone component (exists but underutilized)
2. **SerialPortsSettingsView** - Embedded port discovery
3. **SocatSettingsView** - Embedded device discovery
4. **JobWizardView** - Embedded port discovery

### Current Issues

1. **Code Duplication**: Same ListBox + WrapPanel + ItemTemplate pattern repeated 3+ times
2. **Styling Inconsistencies**: Each view has slightly different styling approaches
3. **Maintenance Burden**: Bug fixes or style changes need to be applied in multiple places
4. **Visual Inconsistency**: The current implementation doesn't match the mockup design
5. **Missing Reusability**: `SerialPortScannerView` exists but is not used by other views

### Visual Comparison

**Desired Look** (from mockup):
- Horizontal card layout with consistent spacing
- Port type badges (USB, ACM, Standard) with icons
- Clean "OK" badge for accessible ports
- Proper wrapping behavior
- Consistent padding and margins

**Current Implementation Issues**:
- ListBoxItem default styling interferes with custom styles
- Inconsistent spacing and padding
- Selection highlight obscures card design
- Margins not properly applied to wrapped items

---

## Solution Approaches

### Option 1: Reusable UserControl (Recommended ⭐)

**Create a dedicated `SerialPortDiscoveryControl.axaml` user control**

#### Advantages
✅ Single source of truth for port discovery UI
✅ Consistent styling and behavior across all views
✅ Easy to maintain and update
✅ Can expose properties for customization
✅ Follows Avalonia best practices
✅ Leverages existing `SerialPortScannerViewModel`

#### Implementation Strategy

```xml
<!-- New: SerialPortDiscoveryControl.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             x:Class="S7Tools.Views.Controls.SerialPortDiscoveryControl"
             x:DataType="vm:SerialPortScannerViewModel">

  <Grid RowDefinitions="Auto,*,Auto">
    <!-- Header with filters and scan button -->
    <StackPanel Grid.Row="0">
      <!-- Filters, scan button, stop button -->
    </StackPanel>

    <!-- Port discovery list with proper styling -->
    <ScrollViewer Grid.Row="1">
      <ListBox ItemsSource="{Binding DiscoveredPorts}"
               SelectedItem="{Binding SelectedPort}">
        <ListBox.ItemsPanel>
          <ItemsPanelTemplate>
            <WrapPanel Orientation="Horizontal" />
          </ItemsPanelTemplate>
        </ListBox.ItemsPanel>

        <!-- Override default ListBoxItem styling -->
        <ListBox.ItemContainerTheme>
          <ControlTheme TargetType="ListBoxItem"
                        BasedOn="{StaticResource {x:Type ListBoxItem}}">
            <Setter Property="Padding" Value="0" />
            <Setter Property="Margin" Value="0,0,12,12" />
            <Setter Property="Background" Value="Transparent" />
          </ControlTheme>
        </ListBox.ItemContainerTheme>

        <ListBox.ItemTemplate>
          <DataTemplate>
            <Border Classes="PortCard">
              <!-- Port card content -->
            </Border>
          </DataTemplate>
        </ListBox.ItemTemplate>
      </ListBox>
    </ScrollViewer>

    <!-- Footer with statistics -->
    <Grid Grid.Row="2">
      <!-- Found/Accessible counts -->
    </Grid>
  </Grid>
</UserControl>
```

#### Key Styling Technique

Use **ItemContainerTheme** to override default ListBoxItem styling:

```xml
<ListBox.ItemContainerTheme>
  <ControlTheme TargetType="ListBoxItem"
                BasedOn="{StaticResource {x:Type ListBoxItem}}">
    <!-- Remove default padding/margin that interferes with card layout -->
    <Setter Property="Padding" Value="0" />
    <Setter Property="Margin" Value="0,0,12,12" />

    <!-- Make background transparent so card shows through -->
    <Setter Property="Background" Value="Transparent" />

    <!-- Optional: Override selection styling -->
    <Style Selector="^:selected">
      <Setter Property="Background" Value="Transparent" />
    </Style>

    <!-- Optional: Custom hover effect on card instead -->
    <Style Selector="^:pointerover Border.PortCard">
      <Setter Property="BorderBrush" Value="#007ACC" />
    </Style>
  </ControlTheme>
</ListBox.ItemContainerTheme>
```

#### Usage in Parent Views

```xml
<!-- SerialPortsSettingsView.axaml -->
<controls:SerialPortDiscoveryControl
    DataContext="{Binding PortScanner}"
    Height="200" />

<!-- SocatSettingsView.axaml -->
<controls:SerialPortDiscoveryControl
    DataContext="{Binding PortScanner}"
    Height="200" />

<!-- JobWizardView.axaml -->
<controls:SerialPortDiscoveryControl
    DataContext="{Binding PortScanner}"
    Height="200" />
```

---

### Option 2: Application-Level Style (Alternative)

**Define global port discovery styles in `App.axaml` or dedicated style file**

#### Advantages
✅ Centralized styling
✅ No new controls to manage
✅ Easy to apply consistently

#### Disadvantages
❌ Still requires duplicating control structure
❌ Behavior logic still scattered
❌ Less encapsulation

#### Implementation

```xml
<!-- App.axaml or Styles/PortDiscoveryStyles.axaml -->
<Styles>
  <!-- Port Discovery ListBox Style -->
  <Style Selector="ListBox.PortDiscoveryList">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="BorderBrush" Value="Transparent" />
  </Style>

  <!-- Port Discovery ListBoxItem Style -->
  <Style Selector="ListBox.PortDiscoveryList > ListBoxItem">
    <Setter Property="Padding" Value="0" />
    <Setter Property="Margin" Value="0,0,12,12" />
    <Setter Property="Background" Value="Transparent" />
  </Style>

  <Style Selector="ListBox.PortDiscoveryList > ListBoxItem:selected">
    <Setter Property="Background" Value="Transparent" />
  </Style>

  <Style Selector="ListBox.PortDiscoveryList > ListBoxItem:pointerover Border.PortCard">
    <Setter Property="BorderBrush" Value="#007ACC" />
  </Style>
</Styles>
```

#### Usage

```xml
<ListBox Classes="PortDiscoveryList"
         ItemsSource="{Binding DiscoveredPorts}">
  <!-- ItemsPanel, ItemTemplate, etc. -->
</ListBox>
```

---

### Option 3: Hybrid Approach (Best of Both Worlds ⭐⭐)

**Combine dedicated UserControl with application-level styles**

#### Implementation

1. **Create reusable control** with proper structure
2. **Define global styles** for consistent theming
3. **Use ItemContainerTheme** for ListBoxItem overrides
4. **Keep card styles** in `PropertyStyles.axaml`

This provides maximum reusability and maintainability.

---

## Recommended Solution: Option 1 + Option 3 Hybrid

### Implementation Plan

#### Phase 1: Extract Reusable Control
1. ✅ Create `SerialPortDiscoveryControl.axaml` and code-behind
2. ✅ Move all port discovery logic to control
3. ✅ Use `SerialPortScannerViewModel` as DataContext
4. ✅ Implement ItemContainerTheme for ListBoxItem styling

#### Phase 2: Centralize Styles
1. ✅ Keep existing `Border.PortCard` style in `PropertyStyles.axaml`
2. ✅ Add application-level overrides in `App.axaml` if needed
3. ✅ Ensure default theme doesn't override custom styles

#### Phase 3: Update Consumer Views
1. ✅ Replace embedded discovery in `SerialPortsSettingsView`
2. ✅ Replace embedded discovery in `SocatSettingsView`
3. ✅ Replace embedded discovery in `JobWizardView`
4. ✅ Update ViewModels to use `SerialPortScannerViewModel`

#### Phase 4: Testing
1. ✅ Verify visual consistency across all views
2. ✅ Test filtering and scanning functionality
3. ✅ Ensure proper data binding
4. ✅ Validate accessibility features

---

## Avalonia Best Practices Reference

### ItemContainerTheme vs Styles

From Avalonia docs:

> **ItemContainerTheme**: Preferred method for styling container items in ListBox, ComboBox, etc.
> Provides better control over the container's default template and allows BasedOn inheritance.

```xml
<!-- Modern approach (Avalonia 11+) -->
<ListBox.ItemContainerTheme>
  <ControlTheme TargetType="ListBoxItem"
                BasedOn="{StaticResource {x:Type ListBoxItem}}">
    <Setter Property="Padding" Value="0" />
  </ControlTheme>
</ListBox.ItemContainerTheme>
```

```xml
<!-- Legacy approach (still works) -->
<ListBox.Styles>
  <Style Selector="ListBoxItem">
    <Setter Property="Padding" Value="0" />
  </Style>
</ListBox.Styles>
```

### Style Precedence Order

1. **Inline properties** (highest priority)
2. **ControlTheme** (ItemContainerTheme)
3. **Local Styles** (control-specific)
4. **Application Styles** (`App.axaml`)
5. **FluentTheme** (lowest priority)

To ensure our custom styles aren't overridden:
- Use `ItemContainerTheme` for container styling
- Place custom styles **after** FluentTheme in `App.axaml`
- Use specific selectors for targeted styling

---

## File Structure

```
src/S7Tools/
├── Views/
│   ├── Controls/
│   │   ├── SerialPortDiscoveryControl.axaml
│   │   └── SerialPortDiscoveryControl.axaml.cs
│   ├── SerialPortScannerView.axaml (keep for standalone use)
│   ├── SerialPortsSettingsView.axaml (updated)
│   ├── SocatSettingsView.axaml (updated)
│   └── JobWizardView.axaml (updated)
├── ViewModels/
│   └── SerialPortScannerViewModel.cs (already exists)
└── Styles/
    └── PropertyStyles.axaml (keep PortCard style)
```

---

## Benefits Summary

### Code Quality
- ✅ **DRY Principle**: Single implementation, multiple uses
- ✅ **Maintainability**: One place to fix bugs or add features
- ✅ **Testability**: Isolated component easier to test

### User Experience
- ✅ **Consistency**: Same look and feel everywhere
- ✅ **Accessibility**: Centralized focus management
- ✅ **Performance**: Optimized rendering with proper ItemsPanel

### Developer Experience
- ✅ **Reusability**: Drop-in control for new views
- ✅ **Documentation**: Clear usage patterns
- ✅ **Standards**: Follows Avalonia best practices

---

## Next Steps

1. **Review this proposal** and select preferred approach
2. **Create implementation tasks** based on chosen solution
3. **Prototype the control** with one view first
4. **Test thoroughly** before rolling out to all views
5. **Document usage** for future developers

---

## Questions for Consideration

1. Should the control support both horizontal and vertical layouts?
2. Do we need configuration properties (e.g., card width, height)?
3. Should scan functionality be optional (e.g., display-only mode)?
4. Do we want to expose styling through properties or rely on styles?
5. Should the control handle its own ViewModel or accept one via DataContext?

**Recommendation**: Keep it simple initially, expose DataContext, rely on styles for theming.
