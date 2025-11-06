# Port Discovery Control - Visual Architecture

## Current Architecture (Duplicated)

```
┌─────────────────────────────────────┐
│ SerialPortsSettingsView.axaml      │
├─────────────────────────────────────┤
│  ┌───────────────────────────────┐  │
│  │ Port Discovery Section        │  │
│  │ ┌───────────────────────────┐ │  │
│  │ │ Header + Scan Button      │ │  │
│  │ ├───────────────────────────┤ │  │
│  │ │ ListBox                   │ │  │
│  │ │  └─ WrapPanel             │ │  │
│  │ │     └─ Port Cards (50L)   │ │  │
│  │ ├───────────────────────────┤ │  │
│  │ │ Footer (Stats)            │ │  │
│  │ └───────────────────────────┘ │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ SocatSettingsView.axaml            │
├─────────────────────────────────────┤
│  ┌───────────────────────────────┐  │
│  │ Port Discovery Section        │  │
│  │ ┌───────────────────────────┐ │  │
│  │ │ Header + Scan Button      │ │  │
│  │ ├───────────────────────────┤ │  │
│  │ │ ListBox                   │ │  │
│  │ │  └─ WrapPanel (Vertical!) │ │  │
│  │ │     └─ Port Cards (50L)   │ │  │  ← Different!
│  │ ├───────────────────────────┤ │  │
│  │ │ Footer (Stats)            │ │  │
│  │ └───────────────────────────┘ │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ JobWizardView.axaml                │
├─────────────────────────────────────┤
│  ┌───────────────────────────────┐  │
│  │ Port Discovery Section        │  │
│  │ ┌───────────────────────────┐ │  │
│  │ │ Header + Scan Button      │ │  │
│  │ ├───────────────────────────┤ │  │
│  │ │ ListBox                   │ │  │
│  │ │  └─ WrapPanel             │ │  │
│  │ │     └─ Port Cards (50L)   │ │  │
│  │ ├───────────────────────────┤ │  │
│  │ │ Footer (Stats)            │ │  │
│  │ └───────────────────────────┘ │  │
│  └───────────────────────────────┘  │
└─────────────────────────────────────┘

TOTAL: ~150 lines of duplicated XAML
```

---

## Proposed Architecture (Reusable)

```
┌─────────────────────────────────────┐
│ SerialPortsSettingsView.axaml      │
├─────────────────────────────────────┤
│  <controls:SerialPortDiscovery     │
│     DataContext="{Binding Scanner}" │  ← 3 lines!
│     Height="200" />                 │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ SocatSettingsView.axaml            │
├─────────────────────────────────────┤
│  <controls:SerialPortDiscovery     │
│     DataContext="{Binding Scanner}" │  ← 3 lines!
│     Height="200" />                 │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ JobWizardView.axaml                │
├─────────────────────────────────────┤
│  <controls:SerialPortDiscovery     │
│     DataContext="{Binding Scanner}" │  ← 3 lines!
│     Height="200" />                 │
└─────────────────────────────────────┘

          ↓ All reference ↓

┌─────────────────────────────────────┐
│ SerialPortDiscoveryControl.axaml   │  ← Single source!
├─────────────────────────────────────┤
│  Grid (3 rows)                      │
│  ├─ Row 0: Header + Filters         │
│  ├─ Row 1: ListBox + WrapPanel      │
│  │          └─ ItemContainerTheme   │  ← Key styling!
│  │             └─ ItemTemplate      │
│  │                └─ Border.PortCard│
│  └─ Row 2: Footer (Stats)           │
└─────────────────────────────────────┘

TOTAL: 3 lines per view + 1 shared control
```

---

## Data Flow

```
┌──────────────────────────────────────┐
│ Parent View                          │
│ (SerialPortsSettingsView)            │
│                                      │
│  ViewModel:                          │
│  ┌────────────────────────────────┐ │
│  │ SerialPortsSettingsViewModel   │ │
│  │                                │ │
│  │  public SerialPortScanner      │ │
│  │    PortScanner { get; }  ──────┼─┼─┐
│  └────────────────────────────────┘ │ │
│                                      │ │
│  View:                               │ │
│  <controls:SerialPortDiscovery ──────┘ │
│     DataContext="{Binding Scanner}" /> │
└──────────────────────────────────────┘

              ↓ Bound to ↓

┌──────────────────────────────────────┐
│ SerialPortDiscoveryControl.axaml    │
│ x:DataType="SerialPortScannerVM"    │
│                                      │
│  Properties Used:                    │
│  • DiscoveredPorts                   │
│  • SelectedPort                      │
│  • IsScanning                        │
│  • IncludeUsbPorts                   │
│  • IncludeAcmPorts                   │
│  • IncludeSerialPorts                │
│  • TotalPortsFound                   │
│  • AccessiblePortsCount              │
│  • StatusMessage                     │
│                                      │
│  Commands Used:                      │
│  • ScanPortsCommand                  │
│  • StopScanCommand                   │
└──────────────────────────────────────┘

              ↓ Uses ↓

┌──────────────────────────────────────┐
│ SerialPortScannerViewModel          │
│                                      │
│  Injected Services:                  │
│  • ISerialPortService                │
│  • ILogger<...>                      │
│                                      │
│  Implements:                         │
│  • Port scanning logic               │
│  • Filtering logic                   │
│  • State management                  │
│  • Command handlers                  │
└──────────────────────────────────────┘
```

---

## Styling Hierarchy

```
┌─────────────────────────────────────────────────┐
│ ListBox                                         │
│ (Background: Transparent)                       │
│                                                 │
│  ┌───────────────────────────────────────────┐ │
│  │ WrapPanel                                 │ │
│  │ (Orientation: Horizontal)                 │ │
│  │                                           │ │
│  │  ┌─────────────────────────────────────┐ │ │
│  │  │ ListBoxItem (ItemContainerTheme)    │ │ │  ← KEY OVERRIDE!
│  │  │ • Padding: 0                        │ │ │
│  │  │ • Margin: 0,0,12,12  ← Card spacing │ │ │
│  │  │ • Background: Transparent           │ │ │
│  │  │                                     │ │ │
│  │  │  ┌───────────────────────────────┐ │ │ │
│  │  │  │ Border.PortCard               │ │ │ │
│  │  │  │ (From PropertyStyles.axaml)   │ │ │ │
│  │  │  │ • Background: #2A2A2B         │ │ │ │
│  │  │  │ • BorderBrush: #464647        │ │ │ │
│  │  │  │ • BorderThickness: 1          │ │ │ │
│  │  │  │ • CornerRadius: 3             │ │ │ │
│  │  │  │ • Padding: 8,6                │ │ │ │
│  │  │  │ • Width: 140                  │ │ │ │
│  │  │  │                               │ │ │ │
│  │  │  │  ┌─────────────────────────┐ │ │ │ │
│  │  │  │  │ StackPanel              │ │ │ │ │
│  │  │  │  │                         │ │ │ │ │
│  │  │  │  │  • Icon + Type Badge    │ │ │ │ │
│  │  │  │  │  • Port Name + OK Badge │ │ │ │ │
│  │  │  │  │                         │ │ │ │ │
│  │  │  │  └─────────────────────────┘ │ │ │ │
│  │  │  └───────────────────────────────┘ │ │ │
│  │  └─────────────────────────────────────┘ │ │
│  │                                           │ │
│  │  [Card 2] [Card 3] [Card 4] ...          │ │
│  │                                           │ │
│  └───────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

**Key Insight:**
- **ListBoxItem** = Spacing container (margin)
- **Border.PortCard** = Visual container (background, border, padding)
- **ItemContainerTheme** overrides default ListBoxItem styling to prevent conflicts

---

## Style Precedence Flow

```
FluentTheme Default ListBoxItem Styling
↓
(Padding: 4, Background: SystemBackground, etc.)
↓
OVERRIDE with ItemContainerTheme ← Our Control
↓
(Padding: 0, Background: Transparent, Margin: 0,0,12,12)
↓
Border.PortCard applies its own styling
↓
(Background: #2A2A2B, Border, Padding, etc.)
↓
RESULT: Clean card appearance ✅
```

**Without ItemContainerTheme:**
```
FluentTheme Default ListBoxItem Styling
↓
(Padding: 4, Background: SystemBackground)
↓
Border.PortCard inside padded container ❌
↓
RESULT: Extra spacing, selection obscures card ❌
```

---

## Interaction States

### Normal State
```
┌────────────────┐
│ ╭────────────╮ │  Border.PortCard
│ │ USB        │ │  • Border: #464647 (gray)
│ │ COM3   OK  │ │  • BorderThickness: 1
│ ╰────────────╯ │
└────────────────┘
     ListBoxItem (transparent)
```

### Hover State
```
┌────────────────┐
│ ╔════════════╗ │  Border.PortCard
│ ║ USB        ║ │  • Border: #007ACC (blue) ← Change!
│ ║ COM3   OK  ║ │  • BorderThickness: 2      ← Change!
│ ╚════════════╝ │
└────────────────┘
     ListBoxItem (still transparent)
```

### Selected State
```
┌────────────────┐
│ ╔════════════╗ │  Border.PortCard
│ ║ USB        ║ │  • Border: #007ACC (blue) ← Persist
│ ║ COM3   OK  ║ │  • BorderThickness: 2      ← Persist
│ ╚════════════╝ │
└────────────────┘
     ListBoxItem (still transparent)
```

**Note:** Interaction states apply to `Border.PortCard`, not `ListBoxItem` background!

---

## File Structure

```
src/S7Tools/
├── Views/
│   ├── Controls/
│   │   ├── SerialPortDiscoveryControl.axaml         ← NEW
│   │   └── SerialPortDiscoveryControl.axaml.cs      ← NEW
│   ├── SerialPortsSettingsView.axaml   (modified)
│   ├── SocatSettingsView.axaml         (modified)
│   └── JobWizardView.axaml             (modified)
├── ViewModels/
│   ├── SerialPortScannerViewModel.cs   (unchanged - reused)
│   ├── SerialPortsSettingsViewModel.cs (inject scanner)
│   ├── SocatSettingsViewModel.cs       (inject scanner)
│   └── JobWizardViewModel.cs           (inject scanner)
└── Styles/
    └── PropertyStyles.axaml            (keep PortCard style)
```

---

## Benefits Comparison

### Code Metrics

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Total XAML lines | ~150 duplicated | ~60 shared | -90 lines |
| Views with duplication | 3 | 0 | -3 |
| Maintenance points | 3 | 1 | -2 |
| Reusable controls | 0 | 1 | +1 |

### Quality Metrics

| Aspect | Before | After |
|--------|--------|-------|
| Visual consistency | ❌ Inconsistent | ✅ 100% consistent |
| Styling conflicts | ❌ ListBoxItem issues | ✅ No conflicts |
| Maintainability | ❌ Update 3 places | ✅ Update 1 place |
| Reusability | ❌ Copy-paste | ✅ Drop-in control |
| Code quality | ⚠️ Duplication | ✅ DRY principle |

---

## Implementation Complexity

### Simple ✅
- Create UserControl structure
- Copy existing working implementation
- Add ItemContainerTheme override
- Update consumer views

### Medium ⚠️
- ViewModel property injection
- DataContext binding
- Testing across views

### Complex ❌
- None! Straightforward refactor

---

## Rollback Strategy

If issues arise:

```
git revert <commit-sha>  ← Restore previous implementation
```

Each view update is a separate commit, allowing granular rollback:

1. `feat: Create SerialPortDiscoveryControl`
2. `refactor: Update SerialPortsSettingsView`  ← Can revert this
3. `refactor: Update SocatSettingsView`         ← Or this
4. `refactor: Update JobWizardView`             ← Or this

**Low Risk:** Can revert individual views without affecting others.

---

## Summary

**Current:** 150 lines of duplicated XAML, inconsistent styling
**Proposed:** 1 reusable control, consistent styling, easy maintenance
**Effort:** 5-8 hours implementation
**Risk:** Low (incremental, reversible)
**Benefit:** High (eliminates duplication, improves quality)

**Recommendation:** ✅ Proceed with implementation
