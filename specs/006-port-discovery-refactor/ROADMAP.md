# Port Discovery Control Refactor - Implementation Roadmap

## Overview

This document outlines the step-by-step implementation plan for refactoring the port discovery functionality into a reusable control.

---

## Phase 1: Create Reusable Control

### Task 1.1: Create Control Structure

**Files to create:**
- `src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml`
- `src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml.cs`

**Actions:**
1. Create `Controls` subdirectory under `Views` if not exists
2. Create UserControl with proper namespace
3. Set DataType to `SerialPortScannerViewModel`

**Code template:**
```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="using:S7Tools.ViewModels.Settings"
             xmlns:converters="using:S7Tools.Converters"
             xmlns:icons="using:Projektanker.Icons.Avalonia"
             xmlns:fa="using:FluentAvalonia.UI.Controls"
             x:Class="S7Tools.Views.Controls.SerialPortDiscoveryControl"
             x:DataType="vm:SerialPortScannerViewModel">

  <Grid RowDefinitions="Auto,*,Auto">
    <!-- Implementation -->
  </Grid>
</UserControl>
```

### Task 1.2: Implement Control Layout

**Structure:**
```
Grid (3 rows)
├── Row 0: Header
│   ├── Title + Status
│   ├── Port type filters (USB, ACM, Standard)
│   └── Scan/Stop buttons
├── Row 1: Port List
│   ├── ScrollViewer
│   └── ListBox with WrapPanel
│       ├── ItemContainerTheme (ListBoxItem styling)
│       └── ItemTemplate (Port cards)
├── Row 2: Footer
    └── Statistics (Found/Accessible counts)
```

**Key components:**
- Filter checkboxes with proper bindings
- Scan/Stop button command bindings
- ListBox with ItemContainerTheme override
- Scanning overlay (ProgressRing)

### Task 1.3: Implement ItemContainerTheme

**Critical styling:**
```xml
<ListBox.ItemContainerTheme>
  <ControlTheme TargetType="ListBoxItem"
                BasedOn="{StaticResource {x:Type ListBoxItem}}">
    <Setter Property="Padding" Value="0" />
    <Setter Property="Margin" Value="0,0,12,12" />
    <Setter Property="Background" Value="Transparent" />

    <Style Selector="^:selected">
      <Setter Property="Background" Value="Transparent" />
    </Style>

    <Style Selector="^:pointerover">
      <Setter Property="Background" Value="Transparent" />
    </Style>

    <Style Selector="^:pointerover Border.PortCard">
      <Setter Property="BorderBrush" Value="#007ACC" />
      <Setter Property="BorderThickness" Value="2" />
    </Style>

    <Style Selector="^:selected Border.PortCard">
      <Setter Property="BorderBrush" Value="#007ACC" />
      <Setter Property="BorderThickness" Value="2" />
    </Style>
  </ControlTheme>
</ListBox.ItemContainerTheme>
```

### Task 1.4: Port ItemTemplate

**Use existing structure from SerialPortScannerView:**
```xml
<ListBox.ItemTemplate>
  <DataTemplate>
    <Border Classes="PortCard">
      <StackPanel Spacing="4">
        <StackPanel Orientation="Horizontal" Spacing="6">
          <icons:Icon Value="fa-solid fa-plug"
                      Width="12" Height="12"
                      Foreground="#9CDCFE" />
          <TextBlock FontWeight="SemiBold"
                     FontSize="11"
                     Foreground="#9CDCFE"
                     Text="{Binding PortTypeDisplay}" />
        </StackPanel>

        <StackPanel Orientation="Horizontal" Spacing="6">
          <TextBlock FontSize="12"
                     Foreground="#CCCCCC"
                     Text="{Binding PortName}" />
          <Border Background="#0E639C"
                  CornerRadius="2"
                  Padding="4,2"
                  IsVisible="{Binding IsAccessible}">
            <TextBlock Text="OK"
                       FontSize="9"
                       Foreground="#FFFFFF"/>
          </Border>
        </StackPanel>
      </StackPanel>
    </Border>
  </DataTemplate>
</ListBox.ItemTemplate>
```

---

## Phase 2: Update ViewModels

### Task 2.1: Update SerialPortsSettingsViewModel

**Changes:**
1. Add `SerialPortScannerViewModel` property
2. Initialize in constructor via DI
3. Wire up to control in view

```csharp
public class SerialPortsSettingsViewModel : ViewModelBase
{
    private readonly ISerialPortScannerViewModel _portScanner;

    public ISerialPortScannerViewModel PortScanner => _portScanner;

    public SerialPortsSettingsViewModel(
        ISerialPortProfileService profileService,
        SerialPortScannerViewModel portScanner, // Inject
        ILogger<SerialPortsSettingsViewModel> logger)
    {
        _portScanner = portScanner;
        // ... rest of initialization
    }
}
```

### Task 2.2: Update SocatSettingsViewModel

**Same pattern as Task 2.1**

### Task 2.3: Update JobWizardViewModel

**Same pattern as Task 2.1**

---

## Phase 3: Update Views

### Task 3.1: Replace in SerialPortsSettingsView.axaml

**Find:**
```xml
<!-- Port Discovery Section -->
<Border>
  <StackPanel>
    <!-- Row 1: Port Discovery Title + Scan Ports Button -->
    <Grid>...</Grid>

    <!-- Row 2: The wrap list with the scanned ports -->
    <Grid>
      <ListBox>...</ListBox>
      <Border IsVisible="{Binding IsScanning}">...</Border>
    </Grid>

    <!-- Row 3: Found ports text -->
    <Grid>...</Grid>
  </StackPanel>
</Border>
```

**Replace with:**
```xml
<!-- Port Discovery Section -->
<Border Background="#252526"
        BorderBrush="#464647"
        BorderThickness="1"
        CornerRadius="4"
        Padding="16">
  <controls:SerialPortDiscoveryControl
      DataContext="{Binding PortScanner}"
      Height="200" />
</Border>
```

**Add namespace:**
```xml
xmlns:controls="using:S7Tools.Views.Controls"
```

### Task 3.2: Replace in SocatSettingsView.axaml

**Find:**
```xml
<!-- Serial Device Discovery Section -->
<Border>
  <StackPanel>
    <!-- Similar structure -->
  </StackPanel>
</Border>
```

**Replace with:**
```xml
<!-- Serial Device Discovery Section -->
<Border Background="#252526"
        BorderBrush="#464647"
        BorderThickness="1"
        CornerRadius="4"
        Padding="16">
  <controls:SerialPortDiscoveryControl
      DataContext="{Binding PortScanner}"
      Height="200" />
</Border>
```

### Task 3.3: Replace in JobWizardView.axaml

**Similar to Tasks 3.1 and 3.2**

---

## Phase 4: Cleanup and Refactor

### Task 4.1: Remove Duplicated Code

**Files to clean:**
- `SerialPortsSettingsView.axaml` - Remove old ListBox structure
- `SocatSettingsView.axaml` - Remove old ListBox structure
- `JobWizardView.axaml` - Remove old ListBox structure

**Lines to remove:** ~50 lines of XAML per view

### Task 4.2: Remove Duplicate ViewModel Properties

**In each consuming ViewModel:**
- Remove local `DiscoveredPorts` properties (if any)
- Remove local `IsScanning` properties (if any)
- Remove local port discovery logic
- Keep only reference to `SerialPortScannerViewModel`

### Task 4.3: Update PropertyStyles.axaml (if needed)

**Verify `Border.PortCard` style:**
```xml
<Style Selector="Border.PortCard">
  <Setter Property="Background" Value="#2A2A2B" />
  <Setter Property="BorderBrush" Value="#464647" />
  <Setter Property="BorderThickness" Value="1" />
  <Setter Property="CornerRadius" Value="3" />
  <Setter Property="Padding" Value="8,6" />
  <Setter Property="Width" Value="140" />
  <Setter Property="Margin" Value="0" />
</Style>
```

**Note:** Margin is now handled by ItemContainerTheme

---

## Phase 5: Service Registration

### Task 5.1: Verify DI Registration

**Check `ServiceCollectionExtensions.cs`:**
```csharp
services.TryAddTransient<SerialPortScannerViewModel>();
```

**Ensure transient lifetime** so each view gets its own instance.

---

## Phase 6: Testing

### Task 6.1: Visual Testing

**Checklist:**
- [ ] Open SerialPortsSettingsView → Port Discovery section visible
- [ ] Open SocatSettingsView → Device Discovery section visible
- [ ] Open JobWizardView → Port Discovery section visible
- [ ] All three views show identical styling
- [ ] Card spacing is consistent (12px right/bottom)
- [ ] Port type badges display correctly
- [ ] "OK" badge appears for accessible ports

### Task 6.2: Functional Testing

**Checklist:**
- [ ] Click "Scan Ports" → Scanning starts
- [ ] Progress indicator shows during scan
- [ ] Port cards populate after scan
- [ ] Click port card → Selection works
- [ ] Hover over card → Border highlights
- [ ] Click "Stop" during scan → Scan stops
- [ ] Port type filters work correctly
- [ ] Statistics update (Found/Accessible)

### Task 6.3: Interaction Testing

**Checklist:**
- [ ] Keyboard navigation works
- [ ] Tab through ports
- [ ] Enter/Space to select
- [ ] Focus visible on selected card
- [ ] Screen reader announces ports correctly

### Task 6.4: Responsiveness Testing

**Checklist:**
- [ ] Resize window → Cards wrap correctly
- [ ] No horizontal scrollbar (unless overflow)
- [ ] Vertical scrollbar appears when needed
- [ ] Cards maintain proper spacing at all sizes

---

## Phase 7: Documentation

### Task 7.1: Update Memory Bank

**File:** `.copilot-tracking/memory-bank/systemPatterns.md`

**Add section:**
```markdown
### Port Discovery Pattern

All views requiring serial port discovery use the reusable `SerialPortDiscoveryControl`.

**Usage:**
```xml
<controls:SerialPortDiscoveryControl
    DataContext="{Binding PortScanner}"
    Height="200" />
```

**ViewModel Requirements:**
- Inject `SerialPortScannerViewModel`
- Expose as property for binding
- No local port discovery logic needed
```

### Task 7.2: Update AGENTS.md

**Add note:**
```markdown
## Port Discovery Control

Use `SerialPortDiscoveryControl` for all port discovery needs.
Never duplicate the ListBox + WrapPanel pattern manually.
```

### Task 7.3: Create Usage Guide

**File:** `docs/controls/SerialPortDiscoveryControl.md`

**Content:**
- Purpose and benefits
- Usage examples
- Property reference
- Styling customization
- Common patterns

---

## Phase 8: Code Review and Merge

### Task 8.1: Self Review

**Checklist:**
- [ ] All files formatted (`dotnet format`)
- [ ] No compiler warnings
- [ ] All tests passing
- [ ] Memory Bank updated
- [ ] Code follows project patterns

### Task 8.2: Commit Strategy

**Commits:**
1. `feat: Create SerialPortDiscoveryControl reusable component`
2. `refactor: Replace port discovery in SerialPortsSettingsView`
3. `refactor: Replace port discovery in SocatSettingsView`
4. `refactor: Replace port discovery in JobWizardView`
5. `docs: Update Memory Bank and documentation`

---

## Rollback Plan

If issues arise:

1. **Revert commits** in reverse order
2. **Keep original views** in git history
3. **Document issues** for future attempts

---

## Success Metrics

### Code Metrics
- [ ] ~150 lines of XAML removed
- [ ] 3 views updated to use control
- [ ] 1 reusable control created
- [ ] 0 new compiler warnings
- [ ] 0 test failures

### Quality Metrics
- [ ] 100% visual consistency across views
- [ ] Styling matches mockup
- [ ] No performance regression
- [ ] Keyboard accessibility maintained

### Maintainability Metrics
- [ ] Single source of truth for port discovery
- [ ] Easy to add new consumer views
- [ ] Clear documentation for usage
- [ ] Follows project architecture patterns

---

## Estimated Timeline

- **Phase 1-2:** 2-3 hours (Control creation + ViewModel updates)
- **Phase 3-4:** 1-2 hours (View updates + cleanup)
- **Phase 5:** 15 minutes (Service registration)
- **Phase 6:** 1-2 hours (Testing)
- **Phase 7:** 30 minutes (Documentation)
- **Phase 8:** 30 minutes (Review + merge)

**Total:** 5-8 hours

---

## Next Steps

1. Review this roadmap with team/stakeholders
2. Get approval to proceed
3. Start with Phase 1: Create control
4. Test incrementally after each phase
5. Update documentation as you go

---

## Questions?

See:
- `PROPOSAL.md` - Detailed solution options
- `STYLING_ANALYSIS.md` - Technical styling details
- `SUMMARY.md` - Executive overview
