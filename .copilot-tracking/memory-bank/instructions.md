# Instructions: S7Tools Project Intelligence

**Last Updated**: Current Session  
**Context Type**: Project-specific patterns, preferences, and learned insights  

## Critical Project Intelligence

### **Memory Bank Usage Rules - CRITICAL**

**NEVER mark tasks as complete without user validation**. This is a fundamental rule that was violated and caused incorrect status tracking.

**Key Learning**: 
- User feedback is the ONLY source of truth for task completion
- Implementation changes do not equal working functionality
- Always wait for user testing and validation before updating task status
- Use "In Progress", "Blocked", or "Not Started" until user confirms functionality

### **User Feedback Integration Pattern**

When user provides feedback on implemented changes:

1. **Update task status immediately** to reflect actual state
2. **Document user feedback verbatim** in progress logs
3. **Investigate why implementations didn't work** as expected
4. **Adjust completion estimates** based on actual complexity
5. **Never assume success** without explicit user confirmation

## Development Workflow Patterns

### **UI Implementation Reality Check**

**Pattern Discovered**: UI changes that appear correct in code may not work as expected in runtime.

**Examples from TASK009**:
- GridSplitter reduced to 1px in XAML but user reports "still too big"
- Hover effects implemented in styles but user reports "still not accentuating color"
- DialogService singleton registered but dialogs still not showing

**Lesson**: Always test UI changes in running application before claiming completion.

### **Dialog Service Integration Complexity**

**Challenge**: ReactiveUI Interactions with Avalonia require precise timing and service instance matching.

**Current Issue**: 
- DialogService registered as singleton in Program.cs
- Interaction handlers registered globally
- But dialogs still not showing for File > Exit and Clear Logs

**Investigation Needed**:
- Verify interaction handler registration timing
- Check if handlers are registered on correct service instance
- Ensure UI thread marshalling for dialog display

### **GridSplitter Styling Challenges**

**Issue**: Standard Avalonia GridSplitter styling may not behave as expected.

**Current Problem**:
- 1px height/width set in styles
- Hover transitions implemented
- But user reports dividers still too thick and no hover effects

**Potential Solutions**:
- Investigate if Avalonia GridSplitter has minimum size constraints
- Check if custom templates are needed for ultra-thin dividers
- Verify hover state selectors are working correctly

## Architecture Patterns

### **Service Registration Patterns**

**Established Pattern**: Use ServiceCollectionExtensions for organized service registration

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddS7ToolsServices(this IServiceCollection services, Action<LogDataStoreOptions> configureOptions)
    {
        // Core services
        services.AddSingleton<IActivityBarService, ActivityBarService>();
        services.AddSingleton<ILayoutService, LayoutService>();
        
        // UI services
        services.AddTransient<IDialogService, DialogService>(); // Note: May need singleton for interactions
        
        // ViewModels
        services.AddTransient<MainWindowViewModel>();
        
        return services;
    }
}
```

**Critical Learning**: DialogService lifetime affects ReactiveUI interaction registration.

### **MVVM Implementation Standards**

**Established Pattern**: All ViewModels use ReactiveUI with proper dependency injection

```csharp
public class ExampleViewModel : ReactiveObject, IDisposable
{
    private readonly IService _service;
    private readonly CompositeDisposable _disposables = new();
    
    public ExampleViewModel(IService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        InitializeCommands();
    }
    
    public ReactiveCommand<Unit, Unit> ExampleCommand { get; private set; }
    
    private void InitializeCommands()
    {
        ExampleCommand = ReactiveCommand.CreateFromTask(ExecuteExampleAsync);
        ExampleCommand.ThrownExceptions
            .Subscribe(ex => /* handle exceptions */)
            .DisposeWith(_disposables);
    }
    
    public void Dispose() => _disposables?.Dispose();
}
```

## User Experience Patterns

### **VSCode-Like Interface Requirements**

**User Expectation**: Interface should closely match VSCode behavior and appearance.

**Key Requirements**:
- Ultra-thin panel dividers (thinner than standard Avalonia defaults)
- Smooth hover effects with accent color transitions
- Context menus instead of checkboxes for column management
- Confirmation dialogs for destructive operations
- Settings persistence between sessions

### **Logging System User Patterns**

**User Workflow**:
1. View real-time logs in bottom panel
2. Filter logs by level, search text, date range
3. Clear logs with confirmation dialog
4. Export logs to various formats
5. Toggle column visibility via context menu

**Critical Issues Identified**:
- Clear logs works but no confirmation dialog
- Export functionality completely broken
- Column visibility still uses checkboxes instead of context menu

## Technical Constraints

### **Avalonia UI Limitations**

**GridSplitter Constraints**:
- May have minimum size constraints preventing ultra-thin appearance
- Hover effects may require custom templates
- Cross-platform behavior may vary

**Dialog System Constraints**:
- ReactiveUI Interactions require precise service instance matching
- Timing of handler registration is critical
- UI thread marshalling required for proper display

### **Cross-Platform Considerations**

**File System Access**:
- Settings persistence must work across Windows, Linux, macOS
- File dialog paths need platform-specific defaults
- Export functionality must handle platform-specific file systems

## Quality Standards

### **Code Quality Requirements**

**Established Standards**:
- All public APIs must have XML documentation
- Nullable reference types enabled throughout
- EditorConfig rules enforced
- SOLID principles applied consistently
- Clean Architecture maintained

### **Testing Requirements**

**Current Gap**: No formal testing framework implemented

**Required Testing Strategy**:
- Unit tests for all services and ViewModels
- Integration tests for critical workflows
- UI tests for dialog interactions
- Cross-platform compatibility tests

## Common Pitfalls

### **Task Status Management**

**CRITICAL PITFALL**: Marking tasks complete without user validation

**Prevention**:
- Always use "In Progress" or "Blocked" until user confirms
- Document user feedback verbatim
- Investigate discrepancies between implementation and user experience
- Adjust estimates based on actual complexity

### **UI Implementation Assumptions**

**PITFALL**: Assuming XAML changes work as expected without runtime testing

**Prevention**:
- Test all UI changes in running application
- Verify cross-platform behavior
- Check edge cases and different screen sizes
- Validate user interactions work as intended

### **Service Lifetime Issues**

**PITFALL**: Incorrect service lifetimes causing interaction failures

**Prevention**:
- Carefully consider service lifetimes (Singleton vs Transient)
- Test service interactions across application lifecycle
- Verify dependency injection resolution works correctly
- Document service lifetime decisions and rationale

## Success Patterns

### **Memory Bank Maintenance**

**Successful Pattern**: Comprehensive documentation with clear status tracking

**Key Elements**:
- Regular updates to progress.md with actual status
- Detailed task tracking with user feedback integration
- Clear separation between implementation and validation
- Honest assessment of completion status

### **User Communication**

**Successful Pattern**: Clear communication about implementation vs validation

**Approach**:
- Explain what was implemented
- Request user testing and feedback
- Acknowledge when implementations don't work as expected
- Adjust plans based on user feedback

## Future Considerations

### **Testing Framework Priority**

**High Priority**: Implement comprehensive testing to catch issues before user testing

**Benefits**:
- Reduce user-reported issues
- Increase confidence in implementations
- Enable refactoring with safety net
- Improve overall code quality

### **UI Framework Evaluation**

**Consideration**: Evaluate if Avalonia limitations require workarounds or alternatives

**Areas to Monitor**:
- GridSplitter customization capabilities
- Dialog system integration complexity
- Cross-platform consistency
- Performance characteristics

---

**Document Status**: Living document capturing project intelligence  
**Next Update**: After significant learning or pattern discovery  
**Owner**: Development Team with AI Assistance  

**Key Reminder**: This document captures hard-learned lessons. Always refer to it before making assumptions about task completion or implementation success.