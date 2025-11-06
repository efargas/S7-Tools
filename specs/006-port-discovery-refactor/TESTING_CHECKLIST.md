# Port Discovery Control - Testing Checklist

**Date**: 2025-11-06
**Phase**: 6 - Visual and Functional Testing
**Tester**: Manual Testing Required

---

## Overview

This checklist provides step-by-step instructions for testing the newly implemented `SerialPortDiscoveryControl` across all three views where it's used.

---

## Prerequisites

- ✅ Application builds successfully (0 errors, 0 warnings)
- ✅ All phases 1-5 complete
- ✅ Application is running

---

## Test 1: SerialPortsSettingsView - Visual Inspection

### Navigation
1. Launch S7Tools application
2. Navigate to **Settings** → **Serial Ports Settings**

### Visual Checks
- [ ] Port Discovery section appears below Profile Management section
- [ ] Section title "Port Discovery" is visible
- [ ] Scan button is visible and styled correctly (blue background, #0E639C)
- [ ] Port cards area (160px height) is visible
- [ ] Footer with statistics is visible
- [ ] Overall styling matches other sections (dark theme, proper spacing)

### Control Structure
- [ ] Control is contained within a Border with:
  - Background: #252526
  - BorderBrush: #464647
  - BorderThickness: 1
  - CornerRadius: 4
  - Padding: 16

---

## Test 2: SocatSettingsView - Visual Inspection

### Navigation
1. From main menu, navigate to **Settings** → **Servers Settings** (Socat)

### Visual Checks
- [ ] Port Discovery section appears after Serial Configuration Info Banner
- [ ] Section title "Port Discovery" is visible
- [ ] Scan button labeled "Scan Devices" is visible
- [ ] Port cards area is visible
- [ ] Footer with statistics is visible
- [ ] Styling consistent with SerialPortsSettingsView

---

## Test 3: JobWizardView - Visual Inspection

### Navigation
1. Navigate to **Jobs** → **Create New Job** (or open Job Wizard)
2. View Step 1: Serial Port Profile Selection

### Visual Checks
- [ ] Port Discovery section appears below Serial Profile Details
- [ ] Section is inside Step 1 container
- [ ] Control styling matches wizard theme (slightly darker background #2D2D2D)
- [ ] Scan button is visible
- [ ] Port cards area is visible

---

## Test 4: Functional Testing - Port Scanning

### Test 4.1: SerialPortsSettingsView
1. Click **"Scan Ports"** button
2. Observe:
   - [ ] Button becomes disabled during scan
   - [ ] Progress spinner overlay appears (semi-transparent with ProgressRing)
   - [ ] After scan completes:
     - [ ] Ports appear as cards in horizontal wrap layout
     - [ ] Each card shows port icon and port name
     - [ ] Footer shows correct count (e.g., "Found 2 accessible port(s)")
   - [ ] Status section at bottom shows: "• X ports discovered"

### Test 4.2: SocatSettingsView
1. Click **"Scan Devices"** button
2. Verify same behavior as Test 4.1

### Test 4.3: JobWizardView
1. In Job Wizard Step 1, click **"Scan Ports"** button
2. Verify same behavior as Test 4.1

---

## Test 5: Functional Testing - Port Selection

### Test 5.1: Click Selection
1. After scanning, click on a port card
2. Verify:
   - [ ] Card shows selection state (blue border)
   - [ ] Footer updates: "Selected: /dev/ttyX"
   - [ ] Status Information section shows: "• Selected: /dev/ttyX"

### Test 5.2: Keyboard Navigation
1. Tab to focus the port list
2. Use arrow keys to navigate between ports
3. Verify:
   - [ ] Keyboard focus is visible
   - [ ] Selection updates with arrow keys
   - [ ] Enter key selects focused port

---

## Test 6: Visual Testing - Card Styling

### Port Cards
For each view, verify port cards have:
- [ ] **Spacing**: 12px margin right and bottom (creates visual gap between cards)
- [ ] **Background**: #2A2A2B (dark gray)
- [ ] **Border**: 1px solid #464647
- [ ] **Border Radius**: 3px (rounded corners)
- [ ] **Padding**: 8px horizontal, 6px vertical
- [ ] **Width**: 140px fixed
- [ ] **Height**: Auto (fits content)

### Port Type Badges
- [ ] USB ports show with appropriate icon
- [ ] Serial ports show with serial icon
- [ ] Icon color: #9CDCFE (light blue)
- [ ] Icon size: 12x12px

### Hover Effects
- [ ] Hover over port card → Blue border appears (#0E639C)
- [ ] No background color change on hover (stays transparent/dark)

### Selection Effects
- [ ] Selected port card has blue border (#0E639C)
- [ ] Selected state is clear and distinct
- [ ] Only one card can be selected at a time

---

## Test 7: Functional Testing - Port Type Filters

### Filter Buttons
In the control header, verify filters (if implemented):
- [ ] "All Ports" button
- [ ] "USB" filter button
- [ ] "Serial" filter button
- [ ] Click filter → only matching ports shown
- [ ] "All Ports" shows all ports again

**Note**: If filters are not yet visible, this is expected - they may be added later.

---

## Test 8: Functional Testing - Stop Scanning

### During Active Scan
1. Click "Scan Ports" button
2. While scanning is in progress, click **"Stop"** button (if available)
3. Verify:
   - [ ] Scanning stops immediately
   - [ ] Overlay disappears
   - [ ] Button re-enables
   - [ ] Partial results are shown (if any)

**Note**: If "Stop" button is not visible during scan, this is expected behavior.

---

## Test 9: Integration Testing - Status Messages

### SerialPortsSettingsView
1. After scanning, check **Status Information** section (bottom of view)
2. Verify it shows:
   - [ ] "• X ports discovered" (where X = actual count)
   - [ ] "• Selected: /dev/ttyX" (or "• No port selected")

### SocatSettingsView
1. After scanning, check **Status Information** section
2. Verify it shows:
   - [ ] "• X devices discovered"
   - [ ] Similar status updates

---

## Test 10: Consistency Testing

### Compare All Three Views
1. Scan ports in SerialPortsSettingsView → note the list
2. Navigate to SocatSettingsView and scan → verify same ports appear
3. Navigate to JobWizardView and scan → verify same ports appear
4. Verify:
   - [ ] Port lists are consistent across all views
   - [ ] Styling is consistent (colors, spacing, fonts)
   - [ ] Behavior is consistent (scan, select, display)

---

## Test 11: State Isolation Testing

### Test Independent Instances
1. Open SerialPortsSettingsView → Scan ports → Select port A
2. Navigate to SocatSettingsView → Scan ports → Select port B
3. Navigate back to SerialPortsSettingsView
4. Verify:
   - [ ] Port A is still selected (state is preserved)
   - [ ] Scan results are still shown
5. Navigate to JobWizardView → Open wizard
6. Verify:
   - [ ] No ports are pre-selected (fresh instance)
   - [ ] Must scan to see ports

**Expected**: Each view maintains its own port scanner state independently.

---

## Test 12: Accessibility Testing

### Keyboard Navigation
- [ ] Tab through all interactive elements in order
- [ ] Port cards are focusable with Tab key
- [ ] Arrow keys navigate between port cards
- [ ] Enter key selects focused port
- [ ] Escape key clears selection (if implemented)

### Screen Reader (Optional)
- [ ] Port cards have readable labels
- [ ] Scan button announces "Scan Ports" or "Scan Devices"
- [ ] Selection state is announced

---

## Test 13: Error Handling

### No Ports Found
1. Run scan on system with no serial ports
2. Verify:
   - [ ] No error message appears
   - [ ] Footer shows "Found 0 accessible port(s)"
   - [ ] Empty state is clear

### Permission Issues (Optional)
1. If possible, test with restricted permissions
2. Verify:
   - [ ] Graceful error handling
   - [ ] User-friendly error message

---

## Test 14: Performance Testing

### Scan Speed
- [ ] Scan completes in reasonable time (< 5 seconds)
- [ ] UI remains responsive during scan
- [ ] No visible lag when displaying results

### Memory
- [ ] No memory leaks after multiple scans
- [ ] Port list updates properly (old results cleared)

---

## Known Issues / Notes

*(Document any issues found during testing)*

- Issue 1:
- Issue 2:
- Issue 3:

---

## Test Results Summary

**Date Tested**: ___________
**Tester**: ___________
**Build Version**: ___________

### Overall Status
- [ ] All visual tests passed
- [ ] All functional tests passed
- [ ] All integration tests passed
- [ ] All accessibility tests passed

### Issues Found
- [ ] No blocking issues
- [ ] Minor issues documented
- [ ] Critical issues require fixes

### Sign-off
- [ ] Ready for Phase 7 (Documentation)
- [ ] Requires additional fixes

---

## Automated Testing Notes

For future automated testing, consider:
1. UI automation tests using Avalonia's testing framework
2. Screenshot comparison tests for visual regression
3. Integration tests for ViewModel → Control binding
4. Unit tests for SerialPortScannerViewModel logic

---

**End of Testing Checklist**
