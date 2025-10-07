---
mode: agent
model: Claude Sonnet 4
---
<!-- markdownlint-disable-file -->
# Implementation Prompt: S7Tools UI Controls & Logging System Fixes

## Implementation Instructions

### Step 1: Create Changes Tracking File

You WILL create `20250107-ui-logging-fixes-changes.md` in #file: ./.copilot-tracking/changes/ if it does not exist.

### Step 2: Execute Implementation

You WILL follow #file: ./.github/instructions/task-implementation.instructions.md
You WILL systematically implement #file: ./.copilot-tracking/plans/20250107-ui-logging-fixes-plan.instructions.md task-by-task
You WILL follow ALL project standards and conventions from #file: ./.copilot-tracking/memory-bank/instructions.md

**CRITICAL**: If ${input:phaseStop:true} is true, you WILL stop after each Phase for user review.
**CRITICAL**: If ${input:taskStop:false} is true, you WILL stop after each Task for user review.

### Step 3: Implementation Priority Order

You WILL implement in this specific order to ensure dependencies are met:

1. **Phase 1: GridSplitter Fixes** - Foundation for all panel interactions
   - Fix bottom panel GridSplitter configuration first
   - Fix sidebar GridSplitter configuration second  
   - Apply consistent styling last

2. **Phase 2: Bottom Panel Behavior** - Core UI functionality
   - Implement proper collapse logic
   - Ensure tab header visibility when collapsed
   - Add smooth animations for professional feel

3. **Phase 3: LogViewer Integration** - Critical functionality connection
   - Connect LogViewer to existing DataStore infrastructure
   - Fix real-time updates for immediate log display
   - Implement proper UI thread marshalling for responsiveness
   - Fix DatePicker controls for filtering functionality

4. **Phase 4: File Dialog Integration** - Settings page functionality
   - Create file dialog service interface
   - Implement Avalonia-specific service
   - Connect to settings page commands
   - Register in DI container

5. **Phase 5: Testing and Validation** - Quality assurance
   - Manual testing of all UI controls
   - LogViewer functionality validation
   - File dialog integration testing

### Step 4: Key Implementation Requirements

**Service Registration Pattern**: You WILL register ALL new services in `ServiceCollectionExtensions.cs` following existing patterns.

**MVVM Compliance**: You WILL ensure all UI logic remains in ViewModels, Views handle only presentation.

**Error Handling**: You WILL implement comprehensive error handling with user-friendly messages for all new functionality.

**UI Thread Safety**: You WILL use `IUIThreadService` for all cross-thread UI operations, especially in LogViewer updates.

**VSCode Design Consistency**: You WILL maintain VSCode-like appearance and behavior throughout all fixes.

### Step 5: Testing Requirements

After each Phase completion, you WILL:
1. Build the solution to ensure no compilation errors
2. Test the specific functionality implemented in that phase
3. Verify no regression in existing functionality
4. Document any issues or deviations in the changes file

### Step 6: Cleanup

When ALL Phases are checked off (`[x]`) and completed you WILL do the following:
  1. You WILL provide a markdown style link and a summary of all changes from #file: ./.copilot-tracking/changes/20250107-ui-logging-fixes-changes.md to the user:
    - You WILL keep the overall summary brief
    - You WILL add spacing around any lists
    - You MUST wrap any reference to a file in a markdown style link
  2. You WILL provide markdown style links to ./.copilot-tracking/plans/20250107-ui-logging-fixes-plan.instructions.md, ./.copilot-tracking/details/20250107-ui-logging-fixes-details.md, and ./.copilot-tracking/research/20250107-ui-logging-fixes-research.md documents. You WILL recommend cleaning these files up as well.
  3. **MANDATORY**: You WILL attempt to delete ./.copilot-tracking/prompts/implement-ui-logging-fixes.prompt.md

## Success Criteria

- [ ] Changes tracking file created and maintained throughout implementation
- [ ] All GridSplitters enable smooth panel resizing functionality
- [ ] Bottom panel properly collapses to show tab headers (35px height) and expands to show full content
- [ ] LogViewer displays real-time log entries from test buttons with proper formatting
- [ ] File dialog buttons in settings open native dialogs and update paths correctly
- [ ] DatePicker controls function properly with dark theme styling
- [ ] All new services registered in ServiceCollectionExtensions.cs
- [ ] UI remains responsive during all operations
- [ ] Project conventions followed throughout implementation
- [ ] Comprehensive error handling implemented
- [ ] Changes file updated continuously with progress and decisions