# Port Discovery Control Refactor - Executive Summary

## Problem

Port discovery UI is duplicated across 3+ views with inconsistent styling that doesn't match the design mockup.

## Proposed Solutions

### 🏆 **Option 1: Reusable UserControl (RECOMMENDED)**

Create `SerialPortDiscoveryControl` that encapsulates all port discovery logic and UI.

**Pros:**
- Single source of truth
- Easy to maintain
- Consistent behavior
- Follows Avalonia best practices

**Implementation:**
```xml
<controls:SerialPortDiscoveryControl
    DataContext="{Binding PortScanner}"
    Height="200" />
```

### ⚡ **Option 2: Application-Level Style**

Define global styles in `App.axaml` for port discovery ListBox.

**Pros:**
- Centralized styling
- No new controls

**Cons:**
- Still requires structure duplication
- Less encapsulation

### 🌟 **Option 3: Hybrid (BEST APPROACH)**

Combine reusable control + application-level styles.

## Key Technical Solution

Use **ItemContainerTheme** to override default ListBoxItem styling:

```xml
<ListBox.ItemContainerTheme>
  <ControlTheme TargetType="ListBoxItem"
                BasedOn="{StaticResource {x:Type ListBoxItem}}">
    <Setter Property="Padding" Value="0" />
    <Setter Property="Margin" Value="0,0,12,12" />
    <Setter Property="Background" Value="Transparent" />
  </ControlTheme>
</ListBox.ItemContainerTheme>
```

This ensures our custom card styling (`Border.PortCard`) isn't overridden by the default theme.

## Implementation Phases

1. **Extract Control**: Create `SerialPortDiscoveryControl.axaml`
2. **Centralize Styles**: Add ItemContainerTheme and necessary overrides
3. **Update Views**: Replace 3 embedded implementations with control
4. **Test**: Verify consistency and functionality

## Impact

- **Code Reduction**: ~150 lines of XAML eliminated
- **Consistency**: 100% visual alignment across all views
- **Maintainability**: Single place to update/fix
- **Quality**: Follows Clean Architecture and Avalonia patterns

## Recommendation

**Proceed with Option 3 (Hybrid)** - Provides best balance of reusability, maintainability, and styling flexibility.

See full proposal: `PROPOSAL.md`
