# Feature Specification: Documentation Validation and Synchronization

**Feature Branch**: `010-docs-validation-sync`
**Created**: 2025-11-11
**Status**: Draft
**Input**: User description: "validate all the documentation is in sync with the real source code, examples, patterns, templates, code style, ..."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Documentation Accuracy Verification (Priority: P1)

As a developer or AI coding agent onboarding to the S7Tools project, I need all documentation (architectural patterns, code examples, naming conventions, and templates) to accurately reflect the current codebase, so that I can confidently implement features following documented patterns without discovering mismatches between docs and reality.

**Why this priority**: Incorrect documentation causes:
- Development delays due to confusion and trial-and-error
- Architectural violations when following outdated patterns
- Test failures when examples don't compile
- Increased support burden from repeated questions

**Independent Test**: Can be fully tested by: (1) Extracting all code examples from documentation, (2) Compiling them against current codebase, (3) Verifying namespace conventions match actual file structure, (4) Checking that documented patterns exist in source code. Delivers immediate value by identifying documentation-code mismatches.

**Acceptance Scenarios**:

1. **Given** documentation contains code examples, **When** validation script extracts and attempts to compile them, **Then** all examples compile successfully against current dependencies
2. **Given** documentation references specific file paths, **When** validation checks file existence, **Then** all referenced files exist at documented locations
3. **Given** documentation describes naming conventions (e.g., `S7Tools.ViewModels.{Category}`), **When** validation scans actual source files, **Then** 100% of files follow documented conventions
4. **Given** documentation lists architectural patterns (Profile Management, Internal Method, etc.), **When** validation searches codebase, **Then** all patterns have corresponding implementations in specified locations
5. **Given** documentation contains cross-references to other docs, **When** validation checks link targets, **Then** all internal links resolve to existing documents

---

### User Story 2 - Pattern Implementation Verification (Priority: P2)

As a code reviewer, I need automated verification that documented patterns (Thread Safety, MVVM, Profile Management, etc.) are actually implemented as described, so that architectural standards are enforced consistently across the codebase.

**Why this priority**: Pattern drift undermines architectural integrity and creates maintenance burden. Automated validation prevents silent violations.

**Independent Test**: Can be tested by: (1) Defining pattern signatures from documentation, (2) Scanning codebase for implementations, (3) Verifying signature compliance (method names, inheritance, interfaces). Delivers value by catching pattern violations before code review.

**Acceptance Scenarios**:

1. **Given** "Internal Method Pattern" documented with public/internal method pairs, **When** validation analyzes services using semaphores, **Then** all semaphore-protected methods follow public→internal delegation pattern
2. **Given** "Unified Profile Management" pattern requiring `IProfileBase` implementation, **When** validation scans profile types, **Then** all profile classes (SerialPortProfile, SocatProfile, etc.) implement `IProfileBase`
3. **Given** "ReactiveUI Pattern" requiring `ReactiveObject` inheritance, **When** validation analyzes ViewModels, **Then** all ViewModel classes inherit from `ReactiveObject`
4. **Given** "Namespace Conventions" documented as `S7Tools.ViewModels.{Category}`, **When** validation checks ViewModel files, **Then** namespaces match documented pattern (Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks)

---

### User Story 3 - Template Usability Validation (Priority: P2)

As a developer creating new features, I need code templates (ViewModel, Service, Test) that compile and run correctly with current dependencies, so that I can scaffold new components without debugging template issues.

**Why this priority**: Broken templates waste developer time and create inconsistent code. Working templates accelerate development.

**Independent Test**: Can be tested by: (1) Instantiating each template with sample data, (2) Compiling generated code, (3) Running basic smoke tests. Delivers value by ensuring templates are production-ready.

**Acceptance Scenarios**:

1. **Given** ViewModel template file exists, **When** developer generates new ViewModel from template, **Then** generated code compiles without errors
2. **Given** Service template references DI interfaces, **When** developer generates service, **Then** all interface references resolve to current Core contracts
3. **Given** Test template uses xUnit patterns, **When** developer generates test file, **Then** tests execute successfully (even if assertions are minimal)
4. **Given** templates include using statements, **When** compilation occurs, **Then** no missing namespace errors appear

---

### User Story 4 - Style Enforcement Verification (Priority: P3)

As a maintainer, I need confirmation that .editorconfig rules documented in code-style.md are actually enforced in CI/CD, and that code examples in docs comply with those same rules, so that style guidelines remain authoritative.

**Why this priority**: Unenforced style rules create "dead letter" documentation. Ensures consistency between documented and actual standards.

**Independent Test**: Can be tested by: (1) Running `dotnet format --verify-no-changes` on docs code examples, (2) Comparing .editorconfig rules to docs/guides/code-style.md, (3) Checking CI scripts for format verification. Delivers value by proving style enforcement is active.

**Acceptance Scenarios**:

1. **Given** code-style.md documents indentation rules, **When** .editorconfig is parsed, **Then** rules match documented standards (C#: 4 spaces, XAML: 2 spaces)
2. **Given** documentation contains C# code blocks, **When** formatting tool runs, **Then** code blocks pass style checks
3. **Given** CI pipeline exists, **When** pipeline configuration is reviewed, **Then** `dotnet format --verify-no-changes` is enforced on every PR

---

### Edge Cases

- **What happens when** a documented pattern no longer exists in code (deprecated pattern)? → Validation should flag it and recommend moving to `docs/archive/` with deprecation notice
- **How does system handle** code examples that use simplified syntax for readability (e.g., omitting error handling)? → Validation should allow "example annotations" (e.g., `// ... error handling omitted for brevity`) to pass compilation checks
- **What happens when** documentation refers to external dependencies (NuGet packages) that change versions? → Validation should check that documented package versions match project files (.csproj)
- **How does system handle** pattern variations (e.g., different valid implementations of same pattern)? → Validation should check for pattern "markers" (interfaces, base classes, attributes) rather than exact implementation
- **What happens when** ViewLocator pattern resolves Views differently than documented? → Validation should test View resolution logic against documented namespace transformation rules

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST extract all C# code blocks from markdown documentation files and validate them for compilation errors
- **FR-002**: System MUST verify that all file paths referenced in documentation (`src/S7Tools/Services/StandardProfileManager.cs`, etc.) exist in the repository
- **FR-003**: System MUST scan all ViewModel files and validate that namespaces follow documented convention `S7Tools.ViewModels.{Category}` where Category ∈ {Base, Controls, Dialogs, Jobs, Layout, Pages, Profiles, Settings, Tasks}
- **FR-004**: System MUST scan all View files and validate that namespaces follow documented convention `S7Tools.Views.{Category}` (mirroring ViewModels)
- **FR-005**: System MUST verify that all profile classes (SerialPortProfile, SocatProfile, PowerSupplyProfile, JobProfile, MemoryRegionProfile) implement `IProfileBase` interface as documented
- **FR-006**: System MUST validate that ViewLocator namespace transformation logic matches documented pattern (`.ViewModels.` → `.Views.`, `ViewModel` → `View`)
- **FR-007**: System MUST check that all documented architectural patterns have corresponding implementations:
  - Profile Management Pattern → `StandardProfileManager<T>` exists
  - Internal Method Pattern → Services with semaphores use public/internal method pairs
  - Resource Coordination Pattern → `ResourceCoordinator` service exists
  - Custom Exceptions Pattern → `S7Tools.Core.Exceptions` namespace contains documented exception types
  - Reusable Controls Pattern → `SerialPortDiscoveryControl` exists as documented
- **FR-008**: System MUST verify that all code templates (ViewModel, Service, Test) compile successfully when instantiated
- **FR-009**: System MUST compare .editorconfig rules to code-style.md documentation and report any discrepancies
- **FR-010**: System MUST validate that all internal documentation links (relative paths) resolve to existing files
- **FR-011**: System MUST check that documented service registration patterns match actual DI configuration in `ServiceCollectionExtensions.cs`
- **FR-012**: System MUST generate a validation report with:
  - Total number of documentation files validated
  - Number of code examples checked
  - Number of file path references verified
  - Number of namespace convention violations
  - Number of pattern implementation mismatches
  - List of broken links
  - List of compilation errors (if any)

### Key Entities

- **DocumentationFile**: Markdown file containing patterns, examples, or guides (path, content, code_blocks[], file_references[], links[])
- **CodeExample**: C# code block extracted from documentation (source_file, line_number, code_content, compilation_result, errors[])
- **FilePathReference**: Documented file path that must exist (referenced_path, exists_in_repo, actual_path_if_different)
- **NamespaceValidation**: Validation result for namespace convention (file_path, actual_namespace, expected_pattern, is_compliant)
- **PatternImplementation**: Verification of documented pattern in code (pattern_name, documented_location, actual_files[], signature_match)
- **ValidationReport**: Summary of all validation checks (timestamp, files_checked, violations[], warnings[], recommendations[])
- **EditorConfigRule**: Style rule from .editorconfig (rule_id, setting, documented_value, actual_value, matches)

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of code examples in documentation compile successfully without errors (currently unknown baseline - establish in validation report)
- **SC-002**: 100% of file path references in documentation resolve to existing files (zero broken file references)
- **SC-003**: 100% of ViewModel and View namespaces comply with documented category-based naming convention
- **SC-004**: All 5 core architectural patterns (Profile Management, Internal Method, Resource Coordination, Custom Exceptions, Reusable Controls) verified to exist in documented locations
- **SC-005**: 100% of internal documentation links (between docs/ files) resolve successfully (zero 404s)
- **SC-006**: All templates (ViewModel, Service, Test) generate compilable code when instantiated
- **SC-007**: Zero discrepancies between .editorconfig rules and documented code style guidelines
- **SC-008**: Validation suite executes in under 60 seconds for full documentation scan (performance target for CI integration)
- **SC-009**: Validation report identifies deprecation candidates - any pattern documented but with <5 implementations flagged for review
- **SC-010**: New developer can follow documented patterns to implement a feature without discovering documentation-code mismatch (measured by: zero "doc bug" issues filed within 30 days of implementation)

---

## Constitution Check

### Impacted Constitutional Principles

This feature affects the following S7Tools Constitution (v1.2.0) principles:

1. **Article II: Clean Architecture Boundaries**
   - Validation must verify that documented dependency flow (Application → Domain ← Infrastructure) matches actual project references
   - Impact: Ensures documentation accurately describes architectural boundaries
   - Compliance: ✅ Validation enforces documented rules, no new boundary violations

2. **Article III: MVVM with ReactiveUI**
   - Validation checks that all ViewModels documented inherit from `ReactiveObject`
   - Impact: Verifies MVVM pattern compliance as described in docs
   - Compliance: ✅ Read-only validation, no changes to pattern

3. **Article IV: Test-First Quality Gates**
   - Validation verifies that test templates compile and execute
   - Impact: Ensures documented testing patterns are usable
   - Compliance: ✅ Validation enforces quality, aligns with test-first principle

4. **Article V: Thread Safety & Concurrency**
   - Validation checks Internal Method Pattern implementation in semaphore-protected services
   - Impact: Verifies thread safety patterns match documentation
   - Compliance: ✅ Read-only verification, no concurrency changes

5. **Article VI: Observability, Versioning & Simplicity**
   - Validation generates structured reports about documentation health
   - Impact: Improves observability of documentation quality
   - Compliance: ✅ Aligns with observability principle, simple validation approach

### Compliance Summary

**Status**: ✅ COMPLIANT - This feature enhances constitutional compliance by ensuring documentation accurately describes the governed architecture. No constitutional amendments required.

### Mitigation

- **Potential Risk**: Validation might flag intentional simplifications in documentation examples (e.g., error handling omitted for brevity)
- **Mitigation**: Allow annotation syntax `// ... simplified for documentation` to bypass strict compilation checks while maintaining readability

---

## Implementation Notes

### Validation Approach

The validation will be implemented as a Python script (aligning with existing `scripts/` directory) that:

1. **Parses markdown files** to extract code blocks, file references, and links
2. **Extracts code examples** and attempts compilation using Roslyn APIs or `dotnet build` on temp projects
3. **Scans source files** using grep/regex patterns to verify namespace conventions
4. **Checks pattern implementations** by looking for documented classes/interfaces/methods
5. **Validates links** using relative path resolution
6. **Compares .editorconfig** with code-style.md by parsing both files
7. **Generates HTML/Markdown report** with findings categorized by severity (Error, Warning, Info)

### Integration Points

- Add to `scripts/validate-all.sh` as final validation step
- Integrate into GitHub Actions CI pipeline (fail PR if errors found)
- Create `docs/.metadata/validation-report.md` as output artifact

### Out of Scope

- **NOT generating corrected documentation automatically** (validation only, humans fix errors)
- **NOT modifying source code** to match documentation (docs describe reality, not prescribe it)
- **NOT validating XAML** against views (C# code and markdown only)
- **NOT checking runtime behavior** (static analysis only, no test execution beyond template compilation)

---

## Assumptions

1. **Documentation is source of truth**: When validation finds mismatch, assume documentation is correct intent but outdated (flag for human review)
2. **Code examples must compile**: Unless explicitly marked as "pseudocode" or "simplified," all code blocks should be valid C#
3. **Templates should be production-ready**: All templates in `docs/templates/` should generate working code with minimal placeholders
4. **Namespace conventions are non-negotiable**: All ViewModels/Views must follow categorized namespace pattern (enforced by ViewLocator)
5. **Validation runs pre-commit**: Developers should run `scripts/validate-all.sh` before pushing (enforced in CI)
6. **Broken links are errors**: Any internal documentation link that returns 404 is a blocker (external links are warnings only)
7. **Pattern deprecation requires migration path**: If a documented pattern no longer exists, docs must be moved to `archive/` with deprecation notice

---

## Dependencies

- **Roslyn Compiler API** or **dotnet CLI** for code compilation validation
- **Python 3.x** for validation script execution
- **Markdown parsing library** (e.g., `markdown-it`, `mistune`, or Python `markdown`)
- **Existing validation scripts** in `scripts/` directory (frontmatter, links, duplicates, orphans)
- **GitHub Actions** for CI integration

---

## Related Documentation

- `docs/INDEX.md` - Master documentation index
- `docs/architecture/overview.md` - System architecture to validate
- `docs/patterns/_index.md` - Pattern catalog with implementation locations
- `docs/patterns/system-patterns.md` - Comprehensive pattern reference
- `docs/guides/code-style.md` - Code style guidelines to validate
- `scripts/validate-frontmatter.py` - Existing validation script pattern
- `scripts/README.md` - Validation script documentation

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.

  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - [Brief Title] (Priority: P1)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently - e.g., "Can be fully tested by [specific action] and delivers [specific value]"]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]
2. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 2 - [Brief Title] (Priority: P2)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]

Constitution Compliance:

- All specs MUST include a short "Constitution Check" (reference `.specify/memory/constitution.md` v1.0.0)
  when the change affects public contracts, DI, or core cross-cutting areas. The check must list impacted
  principles and whether the change is compliant or requires mitigations.

