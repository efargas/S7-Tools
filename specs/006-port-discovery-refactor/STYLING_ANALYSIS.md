# Port Discovery Styling Analysis

## Current Implementation Issues

### Problem 1: ListBoxItem Default Styling Conflicts

**Current Code** (across multiple views):
```xml
<ListBox ItemsSource="{Binding DiscoveredPorts}">
  <ListBox.ItemsPanel>
    <ItemsPanelTemplate>
      <WrapPanel Orientation="Horizontal" />
    </ItemsPanelTemplate>
  </ListBox.ItemsPanel>

  <ListBox.ItemTemplate>
    <DataTemplate>
      <Border Classes="PortCard">
        <!-- Port card content -->
      </Border>
    </DataTemplate>
  </ListBox.ItemTemplate>
</ListBox>
```

**Issues:**
1. **Default padding/margin** from ListBoxItem interferes with card spacing
2. **Selection background** (#007ACC) obscures card design
3. **Hover effects** apply to ListBoxItem instead of Border.PortCard
4. **Focus border** adds unwanted visual noise

### Problem 2: Inconsistent Implementations

#### SerialPortScannerView (Standalone)
```xml
<ListBox ItemContainerTheme="{...}">
  <!-- Has proper ItemContainerTheme override -->
</ListBox>
```
✅ Correct approach but not reused by other views

#### SerialPortsSettingsView
```xml
<ListBox>
  <!-- Missing ItemContainerTheme -->
  <!-- Relies on default styling -->
</ListBox>
```
❌ Default ListBoxItem styling interferes

#### SocatSettingsView
```xml
<ListBox>
  <WrapPanel Orientation="Vertical" ItemWidth="150" ItemHeight="32" />
</ListBox>
```
❌ Different orientation and sizing

#### JobWizardView
```xml
<ListBox>
  <!-- Same structure as SerialPortsSettingsView -->
</ListBox>
```
❌ Duplicate code, same styling issues

---

## Solution: ItemContainerTheme Override

### Why ItemContainerTheme?

From Avalonia 11+ documentation:
> ItemContainerTheme provides better control over container styling than Styles.
> It allows BasedOn inheritance and proper template overrides.

### Implementation

```xml
<ListBox ItemsSource="{Binding DiscoveredPorts}"
         SelectedItem="{Binding SelectedPort}"
         Background="Transparent"
         BorderBrush="Transparent">

  <!-- ItemsPanel for horizontal wrapping -->
  <ListBox.ItemsPanel>
    <ItemsPanelTemplate>
      <WrapPanel Orientation="Horizontal" />
    </ItemsPanelTemplate>
  </ListBox.ItemsPanel>

  <!-- Override ListBoxItem styling -->
  <ListBox.ItemContainerTheme>
    <ControlTheme TargetType="ListBoxItem"
                  BasedOn="{StaticResource {x:Type ListBoxItem}}">
      <!-- Remove default padding that interferes with card -->
      <Setter Property="Padding" Value="0" />

      <!-- Set margin for card spacing (right: 12px, bottom: 12px) -->
      <Setter Property="Margin" Value="0,0,12,12" />

      <!-- Transparent background so card shows through -->
      <Setter Property="Background" Value="Transparent" />

      <!-- Remove selection background -->
      <Style Selector="^:selected">
        <Setter Property="Background" Value="Transparent" />
      </Style>

      <!-- Apply hover effect to card, not container -->
      <Style Selector="^:pointerover">
        <Setter Property="Background" Value="Transparent" />
      </Style>

      <!-- Custom hover on card border -->
      <Style Selector="^:pointerover Border.PortCard">
        <Setter Property="BorderBrush" Value="#007ACC" />
        <Setter Property="BorderThickness" Value="2" />
      </Style>

      <!-- Custom selection on card border -->
      <Style Selector="^:selected Border.PortCard">
        <Setter Property="BorderBrush" Value="#007ACC" />
        <Setter Property="BorderThickness" Value="2" />
      </Style>
    </ControlTheme>
  </ListBox.ItemContainerTheme>

  <!-- ItemTemplate defines card content -->
  <ListBox.ItemTemplate>
    <DataTemplate>
      <Border Classes="PortCard">
        <StackPanel Spacing="4">
          <!-- Port type badge -->
          <StackPanel Orientation="Horizontal" Spacing="6">
            <icons:Icon Value="fa-solid fa-plug"
                        Width="12" Height="12"
                        Foreground="#9CDCFE" />
            <TextBlock FontWeight="SemiBold"
                       FontSize="11"
                       Foreground="#9CDCFE"
                       Text="{Binding PortTypeDisplay}" />
          </StackPanel>

          <!-- Port name and status -->
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
</ListBox>
```

### PropertyStyles.axaml (keep existing)
```xml
<Style Selector="Border.PortCard">
  <Setter Property="Background" Value="#2A2A2B" />
  <Setter Property="BorderBrush" Value="#464647" />
  <Setter Property="BorderThickness" Value="1" />
  <Setter Property="CornerRadius" Value="3" />
  <Setter Property="Padding" Value="8,6" />
  <Setter Property="Width" Value="140" />
  <Setter Property="Margin" Value="0" /> <!-- Margin handled by ItemContainerTheme -->
</Style>
```

---

## Visual Hierarchy

```
ListBox (transparent background)
└── WrapPanel (horizontal wrapping)
    └── ListBoxItem (transparent, 0 padding, 0,0,12,12 margin)
        └── Border.PortCard (styled card with background, border, padding)
            └── Card Content (icon, text, badges)
```

**Key Insight:**
- ListBoxItem provides **spacing** (margin)
- Border.PortCard provides **visual styling** (background, border, padding)
- This separation prevents conflicts

---

## Style Precedence

1. **ItemContainerTheme** (highest for ListBoxItem)
2. **Classes** (e.g., `Border.PortCard`)
3. **Local Styles**
4. **App.axaml Styles**
5. **FluentTheme** (lowest)

By using ItemContainerTheme, we ensure our overrides take precedence over FluentTheme defaults.

---

## Comparison Table

| Feature | Current (Broken) | Proposed (Fixed) |
|---------|------------------|------------------|
| **Spacing** | Inconsistent, margin inside Border | Consistent, margin on ListBoxItem |
| **Selection** | Blue background obscures card | Transparent, border highlight |
| **Hover** | Default ListBoxItem hover | Custom card border highlight |
| **Focus** | Default focus rectangle | Clean, card-based indication |
| **Reusability** | Duplicated 3x | Single control |
| **Maintenance** | Update 3 places | Update 1 place |

---

## Alternative Approaches (Not Recommended)

### ❌ Approach 1: Style Selector
```xml
<ListBox.Styles>
  <Style Selector="ListBoxItem">
    <Setter Property="Padding" Value="0" />
  </Style>
</ListBox.Styles>
```
**Problem:** Lower precedence than FluentTheme, may not apply

### ❌ Approach 2: Control-Level Styles
```xml
<Style Selector="ListBox#PortsListBox > ListBoxItem">
```
**Problem:** Requires unique names, harder to reuse

### ✅ Approach 3: ItemContainerTheme (Recommended)
```xml
<ListBox.ItemContainerTheme>
  <ControlTheme TargetType="ListBoxItem"
                BasedOn="{StaticResource {x:Type ListBoxItem}}">
```
**Benefit:** Highest precedence, proper inheritance, reusable

---

## Testing Checklist

After implementing the solution:

- [ ] Visual consistency across all 3 views
- [ ] Port cards display with correct spacing (12px right/bottom)
- [ ] Hover changes card border, not background
- [ ] Selection changes card border, not background
- [ ] No visual artifacts from default ListBoxItem styling
- [ ] Wrapping behavior correct at different window sizes
- [ ] Scanning overlay displays correctly
- [ ] Filters work properly
- [ ] Accessibility (keyboard navigation) still functional

---

## References

- Avalonia Docs: [ItemsControl Styling](https://docs.avaloniaui.net/docs/basics/user-interface/controls/itemscontrol)
- Avalonia Docs: [Control Themes](https://docs.avaloniaui.net/docs/styling/control-themes)
- Project: `PropertyStyles.axaml` - Existing card styles
- Project: `SerialPortScannerView.axaml` - Correct implementation example
