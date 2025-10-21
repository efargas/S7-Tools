# Feature Specification: Enhanced Job Information Display

**Feature Branch**: `003-job-info-display`
**Created**: 2025-10-21
**Status**: Draft
**Input**: User description: "in the main jobs view, after the jobs list is and info viewer for the selected job, it must include all the information anout the job: Wich profiles are selected and its properties. the same in wizard view, the info must show all the properties related to the selected profile. this properties are the ones that are included in the edit dialogs."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Complete Job Details in Main View (Priority: P1)

When a user selects a job in the main jobs list, they need to see all relevant job information including which profiles are selected and the detailed properties of each profile. This eliminates the need to open edit dialogs just to view job configuration details.

**Why this priority**: This is the primary workflow - users frequently need to review job configurations to understand what a job will do before executing it. This is essential for job verification and troubleshooting.

**Independent Test**: Can be fully tested by selecting any job in the jobs list and verifying that all job details (basic info, profiles, timing, paths) are displayed in a dedicated info panel, delivering immediate job visibility without additional navigation.

**Acceptance Scenarios**:

1. **Given** jobs exist in the jobs list, **When** user selects a job, **Then** an info panel displays all job details including selected profiles and their key properties
2. **Given** a job has all profile types configured (Serial, Socat, Power Supply), **When** user selects the job, **Then** the info panel shows details for each profile type with their specific properties
3. **Given** a job has no profiles selected, **When** user selects the job, **Then** the info panel clearly indicates which profiles are missing or not configured

---

### User Story 2 - View Selected Profile Details in Job Wizard (Priority: P2)

When creating or editing a job using the wizard, users need to see detailed properties of the currently selected profile at each step. This helps users verify they've chosen the correct profile and understand what settings will be applied.

**Why this priority**: This enhances the wizard experience by providing immediate feedback about profile selections, reducing configuration errors and improving user confidence during job creation.

**Independent Test**: Can be tested by navigating through the job wizard steps and verifying that selecting different profiles in the ComboBox immediately updates the profile details display with all relevant properties.

**Acceptance Scenarios**:

1. **Given** user is on wizard Step 1 (Serial), **When** user selects a serial profile, **Then** detailed serial profile properties are displayed (baud rate, parity, stop bits, flow control settings, etc.)
2. **Given** user is on wizard Step 2 (Socat), **When** user selects a socat profile, **Then** detailed socat properties are displayed (TCP port, host, flags, configuration options)
3. **Given** user is on wizard Step 3 (Power Supply), **When** user selects a power profile, **Then** detailed power supply properties are displayed (host, port, device ID, communication settings)
4. **Given** user enters a job name in Step 1, **When** the name is blank or already exists, **Then** the Next button is disabled and validation feedback is shown
5. **Given** user clicks Cancel at any wizard step, **When** confirmation is provided, **Then** job creation is discarded and user returns to Main Jobs View

---

### User Story 3 - Consistent Information Display Across Views (Priority: P3)

The same profile information and job details should be consistently displayed across different views (main jobs view, wizard view, edit dialogs) using the same level of detail and formatting.

**Why this priority**: Consistency improves user experience and reduces confusion. Users shouldn't see different information about the same profile depending on where they access it.

**Independent Test**: Can be tested by comparing profile information displayed in the main jobs view info panel, wizard step details, and edit dialog properties to ensure consistency and completeness.

**Acceptance Scenarios**:

1. **Given** a job with configured profiles, **When** user views job details in main view vs wizard vs edit dialog, **Then** the same profile properties are shown with consistent formatting
2. **Given** a profile has been updated, **When** user views the profile in different contexts, **Then** the updated information is consistently displayed across all views

---

### Edge Cases

- When a profile is deleted but still referenced by a job, the system displays warning indicators with descriptive fallback text (e.g., "Profile not found: SerialProfile1")
- When profile data is corrupted or invalid, the system shows error indicators with the profile name and issue description
- Missing profile references display clear warning messages that identify the specific missing profile by name and type
- Profiles with minimal configuration show available properties and indicate which standard properties are not configured
- When user attempts to create a job with a duplicate name, real-time validation prevents progression and suggests alternatives
- When user cancels wizard partway through, unsaved changes are discarded without affecting existing jobs

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Main jobs view MUST display a detailed information panel when a job is selected
- **FR-002**: Job information panel MUST show basic job details (name, description, creation date, status)
- **FR-003**: Job information panel MUST display all selected profile references with profile names and types
- **FR-004**: Job information panel MUST show detailed properties for each selected profile (same properties available in edit dialogs)
- **FR-005**: Job wizard MUST display detailed properties of currently selected profile in each step
- **FR-006**: Profile details display MUST include all relevant configuration properties from the profile's edit dialog
- **FR-007**: System MUST handle missing or invalid profile references gracefully by displaying warning indicators with descriptive fallback text that identifies the missing profile by name and type
- **FR-008**: Profile information display MUST be read-only in both main view and wizard contexts
- **FR-009**: Information display MUST update immediately when profile selections change
- **FR-010**: System MUST organize profile properties into collapsible groups (Basic Info, Configuration, Advanced) to reduce visual clutter while maintaining full access to information
- **FR-011**: Job wizard Cancel button MUST discard job creation and return user to Main Jobs View
- **FR-012**: Job wizard MUST validate job name in real-time, preventing Next button progression when name is blank or already exists
- **FR-013**: Job wizard Finish button MUST verify complete job configuration before allowing job creation

### Key Entities *(include if feature involves data)*

- **JobProfile**: Contains references to selected profiles and job configuration details
- **SerialPortProfile**: Serial communication settings (baud rate, parity, stop bits, flow control)
- **SocatProfile**: Network bridge configuration (TCP port, host, flags, options)
- **PowerSupplyProfile**: Power management settings (host, port, device ID, protocol settings)
- **MemoryRegionProfile**: Memory dump configuration (start address, length, regions)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view complete job configuration details in under 2 seconds without opening edit dialogs
- **SC-002**: Profile selection in wizard provides immediate visual feedback with detailed properties display
- **SC-003**: 95% of job configuration information is accessible through the main view info panel without additional navigation
- **SC-004**: Users can identify missing or misconfigured profiles instantly through clear visual indicators
- **SC-005**: Profile information displays consistently across all views with 100% property coverage from edit dialogs

### Constitution Compliance

**Constitution Check** (reference `.specify/memory/constitution.md` v1.0.0):

**Impacted Principles**:

- Article III (MVVM with ReactiveUI): New UI components will follow ReactiveUI patterns
- Article II (Clean Architecture): UI changes will not affect domain or infrastructure layers

**Compliance Status**: ✓ COMPLIANT

- Changes are limited to presentation layer (ViewModels and Views)
- No modifications to domain models or service interfaces required
- Follows existing MVVM patterns with reactive property binding
- Maintains clean separation between UI and business logic

**Mitigations**: None required - this is a pure UI enhancement that leverages existing data models and services.

## Clarifications

### Session 2025-10-21

- Q: How should the system display profile information when a job references a profile that has been deleted or is corrupted? → A: Display warning indicators with fallback text (e.g., "Profile not found: SerialProfile1")
- Q: How should the system organize the display of multiple profile properties to avoid overwhelming users while maintaining accessibility? → A: Organize into collapsible groups (Basic Info, Configuration, Advanced)
- Q: How should the Cancel button behave in the job wizard? → A: Discard job creation and return to Main Jobs View
- Q: How should job name validation work to ensure uniqueness and prevent blank names? → A: Validate job name input field directly - prevent Next button when name is blank or not unique

