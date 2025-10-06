---
mode: agent
model: Claude Sonnet 4
---
<!-- markdownlint-disable-file -->
# Implementation Prompt: VSCode-like UI Implementation

## Implementation Instructions

### Step 1: Create Changes Tracking File

You WILL create `20251006-vscode-ui-implementation-changes.md` in #file: ./.copilot-tracking/changes/ if it does not exist.

### Step 2: Execute Implementation

You WILL follow #file: ./.github/instructions/task-implementation.instructions.md
You WILL systematically implement #file: ./.copilot-tracking/plans/20251006-vscode-ui-implementation-plan.instructions.md task-by-task
You WILL follow ALL project standards and conventions

**CRITICAL**: If ${input:phaseStop:true} is true, you WILL stop after each Phase for user review.
**CRITICAL**: If ${input:taskStop:false} is true, you WILL stop after each Task for user review.

### Step 3: Implementation Guidelines

**Phase-by-Phase Approach**:
- You WILL implement each phase completely before moving to the next
- You WILL test each component as it's implemented
- You WILL ensure backward compatibility during transitions
- You WILL maintain working application state at all times

**Service-First Implementation**:
- You WILL implement all services before UI components that depend on them
- You WILL register services in DI container as they're created
- You WILL use interfaces for all service dependencies
- You WILL implement proper error handling and logging

**UI Component Guidelines**:
- You WILL create UserControls for reusable components
- You WILL use proper MVVM patterns with ViewModels
- You WILL implement data binding for all UI interactions
- You WILL follow VSCode color scheme and layout specifications

**Resource Management**:
- You WILL move ALL hardcoded strings to resource files
- You WILL implement proper localization infrastructure
- You WILL use theme resources for all colors and styling
- You WILL ensure resource files are properly embedded

**Thread Safety**:
- You WILL use ConfigureAwait(false) in all library methods
- You WILL implement UI thread marshalling for cross-thread operations
- You WILL use thread-safe collections where appropriate
- You WILL handle async operations properly in ViewModels

**Testing Requirements**:
- You WILL create unit tests for all services as they're implemented
- You WILL test ViewModel behavior and command execution
- You WILL verify key binding functionality
- You WILL test layout behavior and state management

### Step 4: Quality Assurance

**Before Each Phase Completion**:
- You WILL verify all tasks in the phase are completed
- You WILL run the application to ensure functionality
- You WILL check for any compilation errors or warnings
- You WILL update the changes tracking file

**Code Quality Checks**:
- You WILL ensure all code follows project style guidelines
- You WILL verify proper XML documentation on public APIs
- You WILL check for proper error handling and logging
- You WILL validate resource file integration

### Step 5: Cleanup

When ALL Phases are checked off (`[x]`) and completed you WILL do the following:
  1. You WILL provide a markdown style link and a summary of all changes from #file: ./.copilot-tracking/changes/20251006-vscode-ui-implementation-changes.md to the user:
    - You WILL keep the overall summary brief
    - You WILL add spacing around any lists
    - You MUST wrap any reference to a file in a markdown style link
  2. You WILL provide markdown style links to ./.copilot-tracking/plans/20251006-vscode-ui-implementation-plan.instructions.md, ./.copilot-tracking/details/20251006-vscode-ui-implementation-details.md, and ./.copilot-tracking/research/20251006-vscode-ui-implementation-research.md documents. You WILL recommend cleaning these files up as well.
  3. **MANDATORY**: You WILL attempt to delete ./.copilot-tracking/prompts/implement-vscode-ui.prompt.md

## Success Criteria

- [ ] Changes tracking file created and maintained
- [ ] All plan phases implemented with working code
- [ ] VSCode-like interface fully functional
- [ ] Activity bar with selection states and tooltips working
- [ ] Collapsible sidebar and resizable bottom panel implemented
- [ ] Menu system with key bindings functional
- [ ] All strings moved to resource files
- [ ] Thread-safe UI operations implemented
- [ ] Services properly registered in DI container
- [ ] Unit tests created for all services
- [ ] Application maintains performance and responsiveness
- [ ] All detailed specifications satisfied
- [ ] Project conventions followed throughout
- [ ] Changes file updated continuously

## Implementation Notes

**Key Priorities**:
1. **Incremental Development**: Each phase should result in a working application
2. **Service Architecture**: Implement robust service layer before UI components
3. **Resource Management**: Proper localization and theme management from the start
4. **Thread Safety**: All UI operations must be thread-safe
5. **Testing**: Unit tests for services, integration tests for UI behavior

**Critical Success Factors**:
- Maintain application functionality throughout implementation
- Follow VSCode UI specifications exactly
- Implement proper error handling and logging
- Ensure smooth animations and responsive UI
- Create comprehensive test coverage

**Risk Mitigation**:
- Keep backup of working state before major changes
- Test each component thoroughly before integration
- Implement rollback capability for complex changes
- Monitor performance impact of new features