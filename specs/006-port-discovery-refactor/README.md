# Port Discovery Control Refactor - Specification Index

**Status:** Proposal
**Created:** 2025-11-06
**Author:** AI Coding Agent
**Priority:** Medium
**Estimated Effort:** 5-8 hours

---

## Overview

Refactor duplicated serial port discovery UI into a reusable `SerialPortDiscoveryControl` component with proper styling that matches the design mockup.

---

## Documents

### 1. Executive Summary
**File:** [`SUMMARY.md`](./SUMMARY.md)
**For:** Project leads, stakeholders
**Content:** High-level overview, recommended approach, impact summary

### 2. Detailed Proposal
**File:** [`PROPOSAL.md`](./PROPOSAL.md)
**For:** Developers, architects
**Content:** Three solution options with pros/cons, implementation details, Q&A

### 3. Styling Analysis
**File:** [`STYLING_ANALYSIS.md`](./STYLING_ANALYSIS.md)
**For:** UI developers, designers
**Content:** Technical deep-dive into styling issues, ItemContainerTheme solution, visual hierarchy

### 4. Implementation Roadmap
**File:** [`ROADMAP.md`](./ROADMAP.md)
**For:** Implementation team
**Content:** Step-by-step tasks, code templates, testing checklist, timeline

---

## Quick Start

1. **Read:** [`SUMMARY.md`](./SUMMARY.md) for overview
2. **Review:** [`PROPOSAL.md`](./PROPOSAL.md) for detailed options
3. **Understand:** [`STYLING_ANALYSIS.md`](./STYLING_ANALYSIS.md) for technical approach
4. **Implement:** Follow [`ROADMAP.md`](./ROADMAP.md) phase by phase

---

## Problem Statement

Serial port discovery UI is duplicated across 3+ views with inconsistent styling that doesn't match design mockups.

**Affected Views:**
- `SerialPortsSettingsView.axaml`
- `SocatSettingsView.axaml`
- `JobWizardView.axaml`

**Issues:**
- ~150 lines of duplicated XAML
- ListBoxItem default styling conflicts with card design
- Selection/hover effects obscure custom styling
- Inconsistent spacing and layout

---

## Recommended Solution

**Create reusable `SerialPortDiscoveryControl` with ItemContainerTheme overrides**

**Key Features:**
- Single source of truth for port discovery UI
- Proper ItemContainerTheme to override ListBoxItem defaults
- Consistent card-based design matching mockup
- Reusable across all views via DataContext binding

**Benefits:**
- Eliminates code duplication
- Consistent visual appearance
- Easy to maintain and extend
- Follows Avalonia and project best practices

---

## Technical Approach

### 1. UserControl Structure
```xml
<controls:SerialPortDiscoveryControl
    DataContext="{Binding PortScanner}"
    Height="200" />
```

### 2. ItemContainerTheme Override
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

### 3. Card-Based Design
```xml
<Border Classes="PortCard">
  <!-- Port type badge, name, status -->
</Border>
```

---

## Implementation Phases

1. **Create Control** - New UserControl with proper structure
2. **Update ViewModels** - Inject SerialPortScannerViewModel
3. **Update Views** - Replace duplicated code with control
4. **Cleanup** - Remove old code, update styles
5. **Test** - Visual, functional, accessibility testing
6. **Document** - Update Memory Bank, AGENTS.md

**Estimated Time:** 5-8 hours

---

## Success Criteria

### Code Quality
- ✅ ~150 lines of XAML eliminated
- ✅ 1 reusable control created
- ✅ 3 views updated
- ✅ 0 compiler warnings
- ✅ 0 test failures

### Visual Quality
- ✅ Matches design mockup
- ✅ Consistent across all views
- ✅ Proper spacing (12px)
- ✅ Card hover/selection effects

### Maintainability
- ✅ Single source of truth
- ✅ Easy to extend
- ✅ Clear documentation
- ✅ Follows project patterns

---

## Files Affected

### New Files
- `src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml`
- `src/S7Tools/Views/Controls/SerialPortDiscoveryControl.axaml.cs`
- `docs/controls/SerialPortDiscoveryControl.md`

### Modified Files
- `src/S7Tools/Views/SerialPortsSettingsView.axaml` (~50 lines removed)
- `src/S7Tools/Views/SocatSettingsView.axaml` (~50 lines removed)
- `src/S7Tools/Views/JobWizardView.axaml` (~50 lines removed)
- `src/S7Tools/ViewModels/SerialPortsSettingsViewModel.cs`
- `src/S7Tools/ViewModels/SocatSettingsViewModel.cs`
- `src/S7Tools/ViewModels/JobWizardViewModel.cs`
- `.copilot-tracking/memory-bank/systemPatterns.md`
- `AGENTS.md`

### Unchanged Files
- `src/S7Tools/ViewModels/SerialPortScannerViewModel.cs` (already exists)
- `src/S7Tools/Styles/PropertyStyles.axaml` (keep PortCard style)
- `src/S7Tools/Extensions/ServiceCollectionExtensions.cs` (already registered)

---

## Related Specifications

- [Spec 002: Remove Profile List Columns](../002-remove-profile-list-columns/)
- [Spec 004: Job Panel Refactor](../004-job-panel-refactor/)

---

## References

### External Documentation
- [Avalonia ItemsControl Styling](https://docs.avaloniaui.net/docs/basics/user-interface/controls/itemscontrol)
- [Avalonia Control Themes](https://docs.avaloniaui.net/docs/styling/control-themes)
- [Avalonia WrapPanel](https://docs.avaloniaui.net/docs/reference/controls/wrappanel)

### Project Documentation
- [Pattern Catalog](../../docs/patterns/_index.md) - Project patterns
- [AI Agent Guide](../../docs/guides/ai-agent-guide.md) - Agent guidelines
- [System Patterns](../../docs/patterns/system-patterns.md) - System patterns

---

## Decision Log

### 2025-11-06: Initial Proposal
- **Decision:** Create reusable UserControl with ItemContainerTheme approach
- **Rationale:** Best balance of reusability, maintainability, and styling control
- **Alternative Considered:** Application-level styles only (rejected: still duplicates structure)
- **Status:** Awaiting approval

---

## Open Questions

1. Should the control support both horizontal and vertical layouts?
   - **Recommendation:** Start with horizontal only, extend if needed

2. Should scan functionality be optional (display-only mode)?
   - **Recommendation:** Keep scanning always enabled, simplifies API

3. Should the control expose configuration properties?
   - **Recommendation:** Expose minimal props, rely on DataContext binding

4. Should we keep `SerialPortScannerView` standalone?
   - **Recommendation:** Yes, for direct navigation scenarios

---

## Review Checklist

Before implementation:
- [ ] Review SUMMARY.md for overview
- [ ] Review PROPOSAL.md for options
- [ ] Review STYLING_ANALYSIS.md for technical details
- [ ] Review ROADMAP.md for implementation plan
- [ ] Approve recommended solution
- [ ] Assign implementation tasks
- [ ] Set timeline and milestones

---

## Contact

For questions or clarifications:
- See Documentation Index: `docs/INDEX.md`
- See Pattern Catalog: `docs/patterns/_index.md`
- See AI Agent Guide: `docs/guides/ai-agent-guide.md`

---

**Ready to proceed?** Start with Phase 1 in [`ROADMAP.md`](./ROADMAP.md)
