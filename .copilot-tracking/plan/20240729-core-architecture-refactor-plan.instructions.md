---
applyTo: './.copilot-tracking/changes/20240729-core-architecture-refactor-changes.md'
---
<!-- markdownlint-disable-file -->
# Task Checklist: Core Architecture Refactor

## Overview

This plan refactors the solution to create a `S7Tools.Core` library, separating business logic from the UI and establishing foundational service patterns.

## Objectives

- Create and configure the `S7Tools.Core` class library.
- Move shared components (`Models`, `Exceptions`) to the new core library.
- Define interfaces for S7 communication (Provider and Repository patterns).
- Integrate the new core services into the main application's ViewModel using `ReactiveCommand`.

## Research Summary

- #file: ./.copilot-tracking/research/20240729-core-architecture-refactor-research.md - This plan is based on the analysis and strategy defined in the research file.

## Implementation Checklist

### [ ] Phase 1: Project Restructuring

- [ ] Task 1.1: Create the `S7Tools.Core` class library project.
  - Details: ./.copilot-tracking/details/20240729-core-architecture-refactor-details.md (Lines 7-15)
- [ ] Task 1.2: Add a project reference from `S7Tools` to `S7Tools.Core`.
  - Details: ./.copilot-tracking/details/20240729-core-architecture-refactor-details.md (Lines 17-25)
- [ ] Task 1.3: Move `Models` and `Exceptions` folders to `S7Tools.Core`.
  - Details: ./.copilot-tracking/details/20240729-core-architecture-refactor-details.md (Lines 27-34)

### [ ] Phase 2: Core Service and Interface Definition

- [ ] Task 2.1: Create the `Tag` model in `S7Tools.Core`.
  - Details: ./.copilot-tracking/details/20240729-core-architecture-refactor-details.md (Lines 38-48)
- [ ] Task 2.2: Create the `IS7ConnectionProvider` and `ITagRepository` interfaces.
  - Details: ./.copilot-tracking/details/20240729-core-architecture-refactor-details.md (Lines 50-71)

### [ ] Phase 3: ViewModel and DI Integration

- [ ] Task 3.1: Update `MainWindowViewModel` to use a new `ReactiveCommand` with core services.
  - Details: ./.copilot-tracking/details/20240729-core-architecture-refactor-details.md (Lines 75-101)
- [ ] Task 3.2: Register new services in `Program.cs`.
  - Details: ./.copilot-tracking/details/20240729-core-architecture-refactor-details.md (Lines 103-120)
