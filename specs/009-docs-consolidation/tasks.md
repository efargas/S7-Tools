# Tasks: Documentation Consolidation and Reorganization

**Input**: Design documents from `/specs/009-docs-consolidation/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Constitution Compliance** (Reference: `.specify/memory/constitution.md` v1.2.0):

- **Test-First Quality Gates**: Validation scripts serve as automated "tests" for documentation quality (link validation, metadata checks, duplicate detection)
- **Clean Architecture**: Not applicable (documentation-only feature)
- **Thread Safety**: Not applicable (documentation-only feature)
- **Service Registration**: Not applicable (documentation-only feature)
- **Versioning & Simplicity**: Documentation uses semantic versioning; simple Markdown + validation scripts (YAGNI applied)

**Organization**: Tasks are grouped by user story to enable independent implementation and delivery of each increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- Documentation root: `docs/`
- Validation scripts: `scripts/`
- Migration tracking: `docs/.metadata/`
- Deprecated locations: `.copilot-tracking/`, `reviews/` (to be removed after migration)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize consolidated documentation structure and install required tools

- [X] T001 Create consolidated documentation directory structure at `docs/` with subdirectories: `architecture/`, `patterns/`, `guides/`, `templates/`, `reviews/`, `archive/`
- [X] T002 Create subdirectories: `docs/architecture/decisions/`, `docs/patterns/examples/`, `docs/guides/migration/`, `docs/templates/ui-integration/`, `docs/reviews/archive/`
- [X] T003 [P] Create metadata directory at `docs/.metadata/` for generated manifests and reports
- [X] T004 [P] Install Node.js dependencies: `npm install -g markdown-link-check markdownlint-cli` (Documented; Node.js 20+ required)
- [X] T005 [P] Install Python dependencies: `pip install pyyaml` (for frontmatter parsing) (Installed in .venv)
- [X] T006 [P] Create `.markdown-link-check.json` configuration file at repository root
- [X] T007 [P] Create `scripts/` directory for validation scripts
- [X] T008 [P] Create `docs/.metadata/migration-log.json` with initial empty migrations array
- [X] T009 [P] Create placeholder README files in each category directory explaining purpose (Created scripts/README.md)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core validation infrastructure and analysis tools that ALL user stories depend on

**⚠️ CRITICAL**: No user story migration work can begin until validation scripts are operational

### Validation Script Implementation

- [X] T010 [P] Implement frontmatter validator script at `scripts/validate-frontmatter.py` with JSON/text output modes
- [X] T011 [P] Implement duplicate detector script at `scripts/detect-duplicates.py` with MD5 hashing and difflib similarity analysis
- [X] T012 [P] Implement orphan detector script at `scripts/detect-orphans.py` with exclude pattern support
- [X] T013 [P] Implement cross-reference generator script at `scripts/generate-cross-references.py` with dry-run mode
- [X] T014 [P] Implement migration tracker script at `scripts/track-migration.py` with add/report/status commands
- [X] T015 Create wrapper script `scripts/validate-all.sh` that runs all validation checks in sequence

### Content Analysis

- [ ] T016 Run duplicate detector on current documentation locations: `.copilot-tracking/`, `docs/`, `reviews/`, root-level docs
- [ ] T017 Generate content inventory spreadsheet listing all files with: path, size, last-modified, content hash, category proposal
- [ ] T018 Manually review duplicate detection report and mark files for consolidation vs preservation
- [ ] T019 Create deduplication mapping in `docs/.metadata/deduplication-plan.json` with merge decisions

**Checkpoint**: Validation infrastructure ready - user story implementation can now begin ✅

---

## Phase 3: User Story 1 - AI Agent Rapid Onboarding (Priority: P1) 🎯 MVP

**Goal**: Enable AI agents to locate any architectural pattern or coding standard within 30 seconds by creating a master documentation index and consolidating core patterns/architecture docs

**Independent Test**: Provide AI agent with only `docs/INDEX.md` as entry point, measure time to locate specific pattern (e.g., "Profile Management"), verify <30 seconds

### US1: Validation Tests (Documentation Quality Checks)

**Note**: These validation scripts serve as "tests" equivalent to unit/integration tests for code

- [X] T020 [P] [US1] Create test fixture documentation file with complete frontmatter in `docs/.test-fixtures/complete-doc.md`
- [X] T021 [P] [US1] Create test fixture documentation file with missing frontmatter in `docs/.test-fixtures/incomplete-doc.md`
- [X] T022 [P] [US1] Run frontmatter validator on test fixtures, verify error detection for incomplete file
- [X] T023 [P] [US1] Create test fixture with broken internal link in `docs/.test-fixtures/broken-link.md` (Created complete-doc.md with broken link)
- [X] T024 [P] [US1] Run markdown-link-check on test fixtures, verify broken link detection (Tested with wrapper script)

### US1: Master Documentation Index

- [ ] T025 [US1] Create master documentation index at `docs/INDEX.md` with sections for each category (architecture, patterns, guides, templates, reviews)
- [ ] T026 [US1] Create quick navigation section in `docs/INDEX.md` with AI agent optimization (hierarchical links, search tips)
- [ ] T027 [US1] Create category descriptions in `docs/INDEX.md` explaining when to use each section
- [ ] T028 [US1] Add frontmatter metadata to `docs/INDEX.md` (version: 1.0.0, status: current, tags: [index, navigation])

### US1: Architecture Documentation Consolidation

- [ ] T029 [P] [US1] Migrate `.copilot-tracking/memory-bank/systemPatterns.md` to `docs/patterns/system-patterns.md` using git mv
- [ ] T030 [P] [US1] Migrate `ARCHITECTURE_DIAGRAMS.md` to `docs/architecture/diagrams.md` using git mv
- [ ] T031 [P] [US1] Migrate `PATTERNS_REFERENCE.md` to `docs/patterns/_index.md` using git mv and enhance with category index
- [ ] T032 [P] [US1] Create `docs/architecture/overview.md` consolidating content from `.copilot-tracking/memory-bank/productContext.md` and `Project_Architecture_Blueprint.md`
- [ ] T033 [P] [US1] Create `docs/architecture/clean-architecture.md` extracting Clean Architecture sections from systemPatterns.md
- [ ] T034 [P] [US1] Create `docs/architecture/mvvm-patterns.md` extracting MVVM and ReactiveUI patterns
- [ ] T035 [US1] Add frontmatter metadata to all migrated architecture files (version, created, last-updated, status, tags, related)
- [ ] T036 [US1] Update internal links in architecture files to use new `docs/` paths
- [ ] T037 [US1] Create `docs/architecture/_index.md` as category index linking to all architecture documents

### US1: Core Patterns Documentation

- [ ] T038 [P] [US1] Create `docs/patterns/profile-management.md` extracting Unified Profile Management pattern from systemPatterns.md
- [ ] T039 [P] [US1] Create `docs/patterns/internal-method.md` extracting Internal Method Pattern for semaphores
- [ ] T040 [P] [US1] Create `docs/patterns/resource-coordination.md` extracting Resource Coordination pattern
- [ ] T041 [P] [US1] Create `docs/patterns/custom-exceptions.md` extracting custom exception hierarchy pattern
- [ ] T042 [P] [US1] Create `docs/patterns/reusable-controls.md` extracting reusable UI controls pattern
- [ ] T043 [P] [US1] Add frontmatter metadata to all pattern files with cross-references to related docs
- [ ] T044 [US1] Update `docs/patterns/_index.md` to categorize all patterns with tags and brief descriptions
- [ ] T045 [US1] Run cross-reference generator on patterns directory to create bidirectional links

### US1: AI Agent Onboarding Guide

- [ ] T046 [US1] Migrate `AGENTS.md` to `docs/guides/ai-agent-guide.md` using git mv
- [ ] T047 [US1] Enhance `docs/guides/ai-agent-guide.md` with quick reference section (30-second onboarding workflow)
- [ ] T048 [US1] Add context gathering strategy and key files reference table to AI agent guide
- [ ] T049 [US1] Create `docs/guides/onboarding.md` for human developers (separate from AI guide)
- [ ] T050 [US1] Add frontmatter and cross-references linking onboarding guides to architecture and pattern docs

### US1: Validation and Testing

- [ ] T051 [US1] Run frontmatter validator on all User Story 1 files, fix any missing metadata
- [ ] T052 [US1] Run markdown-link-check on all User Story 1 files, fix any broken links
- [ ] T053 [US1] Run orphan detector on `docs/` directory, verify INDEX.md and category indexes are not flagged
- [ ] T054 [US1] Test AI agent onboarding: have AI read INDEX.md and locate "Profile Management" pattern, measure time (<30 seconds)
- [ ] T055 [US1] Update `docs/.metadata/migration-log.json` with all User Story 1 migrations

**Checkpoint**: AI agents can now navigate from INDEX.md to core patterns and architecture docs within 30 seconds ✅

---

## Phase 4: User Story 2 - Human Developer Documentation Maintenance (Priority: P2)

**Goal**: Enable developers to update documentation in one canonical location with automated cross-reference generation and validation preventing orphans

**Independent Test**: Implement a pattern change, update single documentation file, run validation scripts, verify no orphans created and cross-references automatically updated

### US2: Validation Tests

- [ ] T056 [P] [US2] Create test documentation file that supersedes another in `docs/.test-fixtures/superseding-doc.md`
- [ ] T057 [P] [US2] Run frontmatter validator to verify supersedes relationship validation
- [ ] T058 [P] [US2] Create test orphaned file with no incoming links in `docs/.test-fixtures/orphan-doc.md`
- [ ] T059 [P] [US2] Run orphan detector, verify orphan is detected and reported

### US2: ADR Migration

- [ ] T060 [P] [US2] Migrate `docs/adr/_index.md` to `docs/architecture/decisions/_index.md` using git mv
- [ ] T061 [P] [US2] Migrate `docs/adr/_template.md` to `docs/architecture/decisions/_template.md` using git mv
- [ ] T062 [P] [US2] Migrate `docs/adr/ADR-0001-ui-framework-avalonia-reactiveui.md` to `docs/architecture/decisions/0001-ui-framework.md` using git mv
- [ ] T063 [P] [US2] Migrate `docs/adr/ADR-0002-logging-inmemory-datastore-provider.md` to `docs/architecture/decisions/0002-logging-provider.md` using git mv
- [ ] T064 [US2] Add frontmatter metadata to all ADR files (version, created, status, tags)
- [ ] T065 [US2] Update internal links in ADR files to reference new `docs/` paths

### US2: Code Reviews Migration

- [ ] T066 [P] [US2] Create `docs/reviews/_index.md` as category index with review timeline
- [ ] T067 [P] [US2] Migrate `reviews/LATEST_REVIEW.md` to `docs/reviews/LATEST.md` as symlink or copy using git mv
- [ ] T068 [P] [US2] Migrate `reviews/CODE_QUALITY_IMPROVEMENTS_2025-11-10.md` to `docs/reviews/2025-11-10-quality-improvements.md` using git mv
- [ ] T069 [P] [US2] Migrate `reviews/COMPREHENSIVE_CODE_REVIEW_2025-11-07.md` to `docs/reviews/2025-11-07-comprehensive-review.md` using git mv
- [ ] T070 [US2] Add frontmatter metadata to all review files with cross-references to affected patterns
- [ ] T071 [US2] Create bidirectional links from reviews to patterns (e.g., review mentions Profile Management → link to pattern doc)
- [ ] T072 [US2] Move older reviews to `docs/reviews/archive/` with README explaining archive policy

### US2: Development Guides

- [ ] T073 [P] [US2] Create `docs/guides/development-workflow.md` consolidating terminal commands requirement and daily workflow
- [ ] T074 [P] [US2] Create `docs/guides/testing-guide.md` extracting testing standards from systemPatterns.md and constitution
- [ ] T075 [P] [US2] Create `docs/guides/code-style.md` extracting EditorConfig rules and formatting standards
- [ ] T076 [P] [US2] Create `docs/guides/memory-bank-usage.md` explaining how to maintain Memory Bank (now docs/)
- [ ] T077 [US2] Add frontmatter metadata and cross-references to all guide files

### US2: Automated Cross-Reference Generation

- [ ] T078 [US2] Run cross-reference generator on all `docs/` files to build relationship graph
- [ ] T079 [US2] Generate "Related Documentation" sections at end of each file based on frontmatter and content links
- [ ] T080 [US2] Generate `docs/.metadata/cross-references.json` with full bidirectional link graph
- [ ] T081 [US2] Verify bidirectional links: if A links to B, B should have "Related" section mentioning A

### US2: Validation and Testing

- [ ] T082 [US2] Run frontmatter validator on all User Story 2 files, verify all metadata complete
- [ ] T083 [US2] Run markdown-link-check on all User Story 2 files, verify 100% link validity
- [ ] T084 [US2] Run orphan detector on entire `docs/` directory, verify zero orphans (except intentional like INDEX.md)
- [ ] T085 [US2] Test documentation update: modify a pattern file, run cross-reference generator, verify related sections auto-update
- [ ] T086 [US2] Update `docs/.metadata/migration-log.json` with all User Story 2 migrations

**Checkpoint**: Developers can update single canonical files, cross-references auto-generate, orphan detection prevents drift ✅

---

## Phase 5: User Story 3 - Version-Controlled Pattern Evolution (Priority: P3)

**Goal**: Track documentation version history in frontmatter and git, provide migration guides for deprecated patterns, enable time-based documentation queries

**Independent Test**: Query documentation for specific date, verify correct version retrievable; deprecate a pattern, verify migration guide created with before/after examples

### US3: Validation Tests

- [ ] T087 [P] [US3] Create test documentation file with version history section in `docs/.test-fixtures/versioned-doc.md`
- [ ] T088 [P] [US3] Verify git history preserved for migrated files using `git log --follow`
- [ ] T089 [P] [US3] Create test deprecated file with supersedes relationship in `docs/.test-fixtures/deprecated-doc.md`

### US3: Versioning Infrastructure

- [ ] T090 [US3] Create JSON schema for frontmatter metadata at `docs/.metadata/schema.json` (version 1.0.0)
- [ ] T091 [US3] Enhance frontmatter validator to check semantic versioning format (MAJOR.MINOR.PATCH)
- [ ] T092 [US3] Add version history section template to all documentation type templates
- [ ] T093 [US3] Create `docs/guides/versioning-guide.md` explaining when to increment MAJOR/MINOR/PATCH for docs

### US3: Migration Guides

- [ ] T094 [P] [US3] Create `docs/guides/migration/_index.md` as migration guide category index
- [ ] T095 [P] [US3] Create `docs/guides/migration/deprecated-patterns.md` listing all deprecated patterns with replacements
- [ ] T096 [P] [US3] Create `docs/guides/migration/breaking-changes.md` documenting major version changes in patterns
- [ ] T097 [US3] For each deprecated pattern identified during analysis, add migration entry with before/after examples
- [ ] T098 [US3] Add frontmatter supersedes fields to all deprecated pattern files pointing to replacements

### US3: Archive Organization

- [ ] T099 [US3] Create `docs/archive/_index.md` with archive policy (2-year retention, deprecation reasons)
- [ ] T100 [US3] Move deprecated content from `.copilot-tracking/archive/` to `docs/archive/` using git mv
- [ ] T101 [US3] Add frontmatter to archived files: status=deprecated, archiveDate, removalDate (+2 years), replacement link
- [ ] T102 [US3] Create archive entry table in `docs/archive/_index.md` with deprecation dates and reasons

### US3: Git History Verification

- [ ] T103 [US3] Run verification script to confirm all migrated files have preserved git history (`git log --follow` check)
- [ ] T104 [US3] Generate migration report showing old path → new path → git history status for all migrations
- [ ] T105 [US3] Document git history preservation process in `docs/guides/migration/git-history-preservation.md`

### US3: Validation and Testing

- [ ] T106 [US3] Run frontmatter validator with strict version format checking on all docs
- [ ] T107 [US3] Verify all deprecated files have supersedes field pointing to current replacement
- [ ] T108 [US3] Test version query: use git to checkout specific date, verify documentation reflects patterns current at that time
- [ ] T109 [US3] Test deprecation workflow: mark pattern deprecated, verify migration guide entry created, verify replacement link valid
- [ ] T110 [US3] Update `docs/.metadata/migration-log.json` with archive migrations and deprecation metadata

**Checkpoint**: Documentation versioned, migration paths documented, git history preserved, time-based queries supported ✅

---

## Phase 6: User Story 4 - Cross-Reference Navigation (Priority: P4)

**Goal**: Enable 2-click navigation between any related documentation elements through automatic bidirectional links

**Independent Test**: Start from any pattern documentation, verify all related examples/templates/tests accessible within 2 clicks

### US4: Validation Tests

- [ ] T111 [P] [US4] Create test documentation file with related field in frontmatter in `docs/.test-fixtures/cross-ref-source.md`
- [ ] T112 [P] [US4] Create test documentation file that should be cross-referenced in `docs/.test-fixtures/cross-ref-target.md`
- [ ] T113 [P] [US4] Run cross-reference generator, verify bidirectional link created between source and target

### US4: Templates Migration

- [ ] T114 [P] [US4] Migrate `docs/templates/ui-integration/` directory to `docs/templates/ui-integration/` (already in correct location, add frontmatter)
- [ ] T115 [P] [US4] Create `docs/templates/viewmodel-template.cs` with MVVM pattern boilerplate and usage comments
- [ ] T116 [P] [US4] Create `docs/templates/service-template.cs` with service pattern boilerplate and DI registration example
- [ ] T117 [P] [US4] Create `docs/templates/test-template.cs` with AAA pattern test boilerplate
- [ ] T118 [P] [US4] Create `docs/templates/adr-template.md` for Architecture Decision Records
- [ ] T119 [P] [US4] Create `docs/templates/pattern-template.md` for pattern documentation
- [ ] T120 [US4] Add frontmatter to all templates with cross-references to related patterns
- [ ] T121 [US4] Create `docs/templates/_index.md` as template catalog

### US4: Pattern Examples

- [ ] T122 [P] [US4] Create `docs/patterns/examples/profile-manager-example.cs` demonstrating StandardProfileManager<T> usage
- [ ] T123 [P] [US4] Create `docs/patterns/examples/semaphore-pattern-example.cs` demonstrating Internal Method Pattern
- [ ] T124 [P] [US4] Create `docs/patterns/examples/resource-coordinator-example.cs` demonstrating IResourceCoordinator usage
- [ ] T125 [US4] Add frontmatter to all example files with cross-references to parent pattern docs
- [ ] T126 [US4] Update pattern docs to link to examples in "See Also" or "Examples" sections

### US4: Cross-Reference Enhancement

- [ ] T127 [US4] Enhance cross-reference generator to detect code examples in patterns and auto-link to example files
- [ ] T128 [US4] Enhance cross-reference generator to detect template mentions and auto-link to template files
- [ ] T129 [US4] Enhance cross-reference generator to detect test references and auto-link to test documentation
- [ ] T130 [US4] Re-run cross-reference generator on all files with enhanced detection
- [ ] T131 [US4] Generate visual relationship graph showing pattern → example → template → test links

### US4: Navigation Testing

- [ ] T132 [US4] Test 2-click navigation: start from `docs/patterns/profile-management.md`, verify example reachable in 1 click
- [ ] T133 [US4] Test 2-click navigation: start from pattern, verify template reachable in 2 clicks (pattern → example → template)
- [ ] T134 [US4] Test bidirectional links: verify example file has "Related" section linking back to parent pattern
- [ ] T135 [US4] Run orphan detector, verify all examples and templates have incoming links from patterns or guides

### US4: Validation and Testing

- [ ] T136 [US4] Run frontmatter validator on all templates and examples, verify metadata complete
- [ ] T137 [US4] Run markdown-link-check on all User Story 4 files, verify 100% link validity
- [ ] T138 [US4] Verify pattern coverage: ensure all patterns have at least one linked example and template (SC-010)
- [ ] T139 [US4] Update `docs/.metadata/cross-references.json` with final bidirectional relationship graph
- [ ] T140 [US4] Generate coverage report: pattern → example → template mapping with gap identification

**Checkpoint**: All documentation elements cross-referenced, 2-click navigation verified, 100% pattern coverage ✅

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finalize migration, clean up deprecated locations, set up CI/CD, create redirect stubs

### Redirect Stubs and Transition Period

- [ ] T141 [P] Create redirect stub at `.copilot-tracking/memory-bank/systemPatterns.md` pointing to `docs/patterns/system-patterns.md`
- [ ] T142 [P] Create redirect stub at `AGENTS.md` pointing to `docs/guides/ai-agent-guide.md`
- [ ] T143 [P] Create redirect stub at `PATTERNS_REFERENCE.md` pointing to `docs/patterns/_index.md`
- [ ] T144 [P] Create redirect stub at `reviews/LATEST_REVIEW.md` pointing to `docs/reviews/LATEST.md`
- [ ] T145 Update all redirect stubs with deprecation date (2025-11-10) and removal date (2026-02-15, +3 months)
- [ ] T146 Create calendar reminder task for stub removal date in project tracking system

### CI/CD Integration

- [ ] T147 Create GitHub Actions workflow file at `.github/workflows/docs-validation.yml`
- [ ] T148 Add frontmatter validation job to workflow (blocking gate)
- [ ] T149 Add link validation job to workflow (blocking gate)
- [ ] T150 Add orphan detection job to workflow (warning, non-blocking)
- [ ] T151 Add duplicate detection job to workflow (warning, non-blocking)
- [ ] T152 Configure workflow to run on push and pull_request for any changes to `docs/` directory
- [ ] T153 Test CI/CD workflow: create test PR with broken link, verify workflow fails

### Master Index and Quick Reference

- [ ] T154 Enhance `docs/INDEX.md` with comprehensive table of contents for all categories
- [ ] T155 Add search tips section to `docs/INDEX.md` (grep examples, tag-based search)
- [ ] T156 Create `docs/README.md` as entry point with quick navigation and purpose explanation
- [ ] T157 Add visual diagram to `docs/INDEX.md` showing documentation structure hierarchy

### Documentation Templates

- [ ] T158 Create comprehensive documentation template guide at `docs/guides/documentation-templates.md`
- [ ] T159 Document frontmatter schema with examples in `docs/guides/frontmatter-schema.md`
- [ ] T160 Create contribution guide for documentation updates at `docs/guides/contributing-to-docs.md`

### Final Validation and Metrics

- [ ] T161 Run full validation suite on entire `docs/` directory: frontmatter, links, orphans, duplicates
- [ ] T162 Generate final migration report with statistics: files migrated, duplicates eliminated, links updated
- [ ] T163 Measure success criteria: time to locate pattern (SC-001), duplicate count (SC-002), link validity (SC-003)
- [ ] T164 Create documentation quality dashboard at `docs/.metadata/quality-report.md` with validation results
- [ ] T165 Verify all 10 success criteria from spec.md are measurably met

### Cleanup (After Transition Period)

⚠️ **SCHEDULED FOR 2026-02-15** (3 months after migration)

- [ ] T166 Remove `.copilot-tracking/` directory and all contents
- [ ] T167 Remove root-level `reviews/` directory and all contents
- [ ] T168 Remove deprecated root-level documentation files (AGENTS.md, PATTERNS_REFERENCE.md, ARCHITECTURE_DIAGRAMS.md)
- [ ] T169 Update code comments/references that mention old documentation paths
- [ ] T170 Run final validation sweep to ensure no broken references to removed directories
- [ ] T171 Update `docs/.metadata/migration-log.json` marking migration complete

### User Adoption and Training

- [ ] T172 Create announcement document explaining new documentation structure for team
- [ ] T173 Host documentation walkthrough session demonstrating navigation and update workflow
- [ ] T174 Create FAQ document addressing common questions about new structure
- [ ] T175 Monitor first month usage: track broken link reports, gather feedback on navigation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational (Phase 2) completion
- **User Story 2 (Phase 4)**: Depends on Foundational (Phase 2) completion - Can run in parallel with US1
- **User Story 3 (Phase 5)**: Depends on Foundational (Phase 2) completion - Can run in parallel with US1/US2
- **User Story 4 (Phase 6)**: Depends on Foundational (Phase 2) completion - Should run after US1 (needs master patterns migrated)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Independent - Can start after Foundational phase
- **User Story 2 (P2)**: Independent - Can start after Foundational phase (parallel with US1)
- **User Story 3 (P3)**: Independent - Can start after Foundational phase (parallel with US1/US2)
- **User Story 4 (P4)**: Soft dependency on US1 - Benefits from having patterns migrated first, but could technically run in parallel

### Recommended Sequential Order (MVP-First)

1. **Phase 1** (Setup) → **Phase 2** (Foundational) → Foundation Ready ✅
2. **Phase 3** (US1: AI Agent Onboarding) → MVP Complete 🎯
3. **Phase 4** (US2: Documentation Maintenance) → Maintenance Enabled ✅
4. **Phase 5** (US3: Version Control) → Historical Context Preserved ✅
5. **Phase 6** (US4: Cross-References) → Full Navigation Enabled ✅
6. **Phase 7** (Polish) → Production Ready 🚀

### Parallel Opportunities

- **Phase 1 (Setup)**: T004, T005, T006, T007, T008, T009 can run in parallel (different tools/files)
- **Phase 2 (Foundational)**: T010-T014 (validation scripts) can run in parallel (different files)
- **After Foundational Complete**: US1, US2, US3 can all start in parallel (independent user stories)
- **Within US1**: T029-T034 (architecture migrations) can run in parallel (different files)
- **Within US1**: T038-T042 (pattern creations) can run in parallel (different files)
- **Within US2**: T060-T063 (ADR migrations) can run in parallel (different files)
- **Within US2**: T067-T069 (review migrations) can run in parallel (different files)
- **Within US4**: T114-T120 (template creations) can run in parallel (different files)
- **Within US4**: T122-T125 (example creations) can run in parallel (different files)
- **Phase 7 (Polish)**: T141-T144 (redirect stubs) can run in parallel (different files)

---

## Parallel Example: User Story 1 (AI Agent Onboarding)

```bash
# After Foundational phase completes, launch in parallel:

# Validation tests (run together):
Task T020: Create test fixture with complete frontmatter
Task T021: Create test fixture with missing frontmatter
Task T023: Create test fixture with broken link

# Architecture migrations (run together):
Task T029: Migrate systemPatterns.md → docs/patterns/system-patterns.md
Task T030: Migrate ARCHITECTURE_DIAGRAMS.md → docs/architecture/diagrams.md
Task T031: Migrate PATTERNS_REFERENCE.md → docs/patterns/_index.md
Task T032: Create docs/architecture/overview.md

# Pattern creations (run together):
Task T038: Create docs/patterns/profile-management.md
Task T039: Create docs/patterns/internal-method.md
Task T040: Create docs/patterns/resource-coordination.md
Task T041: Create docs/patterns/custom-exceptions.md
Task T042: Create docs/patterns/reusable-controls.md

# Sequential after parallel groups complete:
Task T036: Update internal links (depends on all migrations)
Task T044: Update pattern index (depends on all pattern creations)
Task T051-T055: Validation and testing (depends on all US1 tasks)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only) 🎯

**Goal**: Enable AI agent rapid onboarding as quickly as possible

1. ✅ Complete Phase 1: Setup (~1-2 hours)
2. ✅ Complete Phase 2: Foundational (~1-2 days - validation scripts are critical)
3. ✅ Complete Phase 3: User Story 1 (~2-3 days)
4. 🛑 **STOP and VALIDATE**: Test AI agent can navigate from INDEX.md to patterns <30 seconds
5. 🚀 Deploy/demo MVP to team, gather feedback

**MVP Delivery**: Core patterns and architecture documented with master index - AI agents can onboard rapidly

### Incremental Delivery

1. **Foundation** (Phase 1+2): Validation infrastructure ready → ~3 days
2. **MVP** (Phase 3): AI agent onboarding enabled → ~2-3 days → Deploy 🚀
3. **Maintenance** (Phase 4): Developer workflows improved → ~2 days → Deploy 🚀
4. **History** (Phase 5): Version control and migration guides → ~2 days → Deploy 🚀
5. **Navigation** (Phase 6): Full cross-reference network → ~2 days → Deploy 🚀
6. **Production** (Phase 7): CI/CD integration, cleanup → ~1 day → Deploy 🚀

**Total Timeline**: ~2-3 weeks for complete implementation

### Parallel Team Strategy

With 3 developers after Foundational phase:

- **Developer A**: User Story 1 (AI Agent Onboarding) - 2-3 days
- **Developer B**: User Story 2 (Documentation Maintenance) - 2 days (parallel)
- **Developer C**: User Story 3 (Version Control) - 2 days (parallel)
- **All together**: User Story 4 (Cross-References) - 2 days (benefits from US1-3 complete)
- **All together**: Polish & CI/CD - 1 day

**Timeline with parallelization**: ~1-1.5 weeks for complete implementation

---

## Task Summary

**Total Tasks**: 175 tasks

**By Phase**:

- Phase 1 (Setup): 9 tasks
- Phase 2 (Foundational): 10 tasks
- Phase 3 (US1 - AI Agent Onboarding): 36 tasks (MVP)
- Phase 4 (US2 - Documentation Maintenance): 31 tasks
- Phase 5 (US3 - Version Control): 24 tasks
- Phase 6 (US4 - Cross-References): 29 tasks
- Phase 7 (Polish): 36 tasks

**Parallel Opportunities Identified**: 47 tasks marked [P] for parallel execution

**Independent Test Criteria**:

- US1: AI agent locates pattern from INDEX.md in <30 seconds
- US2: Single file update, cross-refs auto-generate, zero orphans
- US3: Query docs by date, retrieve correct version; deprecate pattern, migration guide created
- US4: Navigate from any pattern to related materials in ≤2 clicks

**Suggested MVP Scope**: Phase 1 + Phase 2 + Phase 3 (User Story 1 only) = 55 tasks

**Format Validation**: ✅ All tasks follow checklist format: `- [ ] [ID] [P?] [Story?] Description with file path`

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label (US1, US2, US3, US4) maps task to specific user story for traceability
- Each user story is independently completable and testable
- Validation scripts serve as "tests" for documentation quality (constitution compliance)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently before proceeding
- Use `git mv` for all file migrations to preserve history
- All internal links must use relative paths from repository root
- Transition period: 3 months with redirect stubs before cleanup (T166-T171)
