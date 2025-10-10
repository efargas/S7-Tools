---
mode: agent
model: Claude Sonnet 4
---
<!-- markdownlint-disable-file -->
# Implementation Prompt: Siemens S7 Bootloader Integration

## Implementation Instructions

### Step 1: Create Changes Tracking File

Create `20251009-s7-bootloader-integration-changes.md` in `./.copilot-tracking/changes/` if it does not exist.

### Step 2: Execute Implementation

Follow the plan step-by-step:
- Plan: ./.github/agents/workspace/future-plans/20251009-s7-bootloader-integration-plan.md
- Details: ./.github/agents/workspace/future-plans/20251009-s7-bootloader-integration-details.md

Key rules:
- Implement interfaces under S7Tools.Core first
- Add adapters wrapping reference implementations (no behavior change)
- Implement BootloaderService orchestration
- Implement ResourceCoordinator and JobScheduler
- Wire DI in ServiceCollectionExtensions.cs
- Add ViewModels/Views for Task Manager
- Write tests for scheduler, orchestration, adapters

CRITICAL: If ${input:phaseStop:true} is true, stop after each Phase for review. If ${input:taskStop:false} is true, stop after each Task.

### Step 3: Cleanup

When ALL Phases are completed:
1. Provide a summary of changes from ./.copilot-tracking/changes/20251009-s7-bootloader-integration-changes.md
2. Provide links to the plan, details, and research documents
3. Attempt to delete ./.copilot-tracking/prompts/implement-s7-bootloader-integration.prompt.md if present

## Success Criteria

- Interfaces compile in Core with XML docs
- Adapters compile and pass unit tests
- BootloaderService executes end-to-end dump on S7-1200
- Task Manager schedules/queues correctly; UI shows progress and results
- Build succeeds; code style compliant; logs structured
