# [TASK015] - Unify Editor Status Reporting and Name Validation

Status: Pending
Added: 2025-10-16
Updated: 2025-10-16

## Summary

Improve consistency across profile editors (Serial, Socat, PowerSupply) by:
- Unifying error/status reporting via a small interface (e.g., IStatusNotifiable with ReportError/ReportInfo) implemented by editor VMs.
- Centralizing name uniqueness validation within dialog flows for immediate inline feedback.

## Context & Motivation

Recent fixes ensured dialogs close on Save and lists auto-refresh using the refresh-and-reselect pattern. To further polish UX and reduce duplication, we should standardize how editors surface errors and where name uniqueness checks live.

## Acceptance Criteria

- A shared interface or base mixin enables consistent StatusMessage updates from dialog save handlers.
- Duplicate-name validation occurs inside dialog viewmodels with real-time feedback before Save.
- No behavior regressions to existing CRUD flows (Create/Edit/Duplicate/Delete/Set Default).
- Build and tests remain green.

## Implementation Notes

- Define interface in Core or UI layer depending on coupling constraints; prefer UI if it references UI-only patterns (ReactiveObject), otherwise a small Core contract.
- Wire dialog save handlers to call ReportError/ReportInfo rather than direct property access where applicable.
- Add minimal unit tests for name uniqueness validation logic.

## Risks

- Tight coupling between dialogs and specific VM types; mitigate via interface abstraction.
- Validation UX must remain responsive and not block UI thread.

## Verification

- Manual: Attempt create/edit with duplicate names; verify inline error, Save disabled until resolved.
- Automated: Unit tests for validation service or VM logic.
