# Views/ViewModels Folder Reorganization - November 6, 2025

## Summary

Successfully reorganized all Views and ViewModels from flat structure into categorized folders following functional organization principles.

## Changes Made

### 1. Folder Structure

**Before** (Flat structure):
```
src/S7Tools/
  ├── ViewModels/
  │   ├── HomeViewModel.cs
  │   ├── SettingsViewModel.cs
  │   ├── JobsManagementViewModel.cs
  │   └── ... (all ViewModels in one folder)
  └── Views/
      ├── HomeView.axaml
      ├── SettingsView.axaml
      └── ... (all Views in one folder)
```

**After** (Categorized structure):
```
src/S7Tools/
  ├── ViewModels/
  │   ├── Base/              # Base classes (ViewModelBase)
  │   ├── Controls/          # Control ViewModels (PropertyDisplayItem)
  │   ├── Dialogs/           # Dialog ViewModels (Confirmation, Input)
  │   ├── Jobs/              # Job management ViewModels
  │   ├── Layout/            # Layout ViewModels (MainWindow, Navigation, BottomPanel)
  │   ├── Pages/             # Page ViewModels (Home, Connections, LogViewer, About, PlcInput)
  │   ├── Profiles/          # Profile ViewModels (Serial, Socat, PowerSupply)
  │   ├── Settings/          # Settings ViewModels
  │   └── Tasks/             # Task management ViewModels
  └── Views/
      ├── Controls/          # Reusable controls (SidebarSection)
      ├── Dialogs/           # Dialog windows
      ├── Jobs/              # Job-related views
      ├── Layout/            # Layout views (MainWindow, TaskManagerShell)
      ├── Pages/             # Page views
      ├── Profiles/          # Profile edit content views
      ├── Settings/          # Settings views
      └── Tasks/             # Task views
```

### 2. Namespace Updates

**Old Pattern**:
- ViewModels: `S7Tools.ViewModels`
- Views: `S7Tools.Views`

**New Pattern**:
- ViewModels: `S7Tools.ViewModels.{Category}` (e.g., `S7Tools.ViewModels.Pages`)
- Views: `S7Tools.Views.{Category}` (e.g., `S7Tools.Views.Pages`)

### 3. Categories Defined

- **Base**: Foundation classes (ViewModelBase, base implementations)
- **Controls**: Reusable UI control components
- **Dialogs**: Modal dialogs (ConfirmationDialog, InputDialog)
- **Jobs**: Job creation, management, and wizard
- **Layout**: Shell, navigation, bottom panel, main window
- **Pages**: Main content pages (Home, Connections, LogViewer, About, PlcInput)
- **Profiles**: Profile management for Serial, Socat, PowerSupply
- **Settings**: Application settings and configuration
- **Tasks**: Task management, scheduling, execution

### 4. Files Modified

**Total Files Affected**: ~110 files
- ViewModels: ~55 files
- Views (XAML + CodeBehind): ~55 files

**Key Files Updated**:
- All C# namespace declarations updated
- All XAML `x:Class` declarations updated
- All XAML `xmlns` namespace references updated
- `ServiceCollectionExtensions.cs` - Added category using statements
- `ViewLocator.cs` - Works seamlessly with new structure (no changes needed)
- `App.axaml.cs` - Added Dialog namespace using statement

### 5. XAML Namespace Fixes

**Pattern Applied**:
```xaml
<!-- Old -->
xmlns:vm="using:S7Tools.ViewModels"
xmlns:views="using:S7Tools.Views"

<!-- New (category-specific) -->
xmlns:vm="using:S7Tools.ViewModels.Pages"
xmlns:views="using:S7Tools.Views.Pages"

<!-- For files referencing multiple categories -->
xmlns:vmLayout="using:S7Tools.ViewModels.Layout"
xmlns:vmSettings="using:S7Tools.ViewModels.Settings"
xmlns:viewsJobs="using:S7Tools.Views.Jobs"
```

### 6. ViewLocator Pattern

The ViewLocator automatically resolves Views from ViewModels:

```csharp
// Input ViewModel:  S7Tools.ViewModels.Pages.HomeViewModel
// Resolved View:    S7Tools.Views.Pages.HomeView

// Pattern: Replace ".ViewModels." → ".Views." and "ViewModel" → "View"
```

This works seamlessly with the categorized structure without requiring changes to ViewLocator.cs.

### 7. Additional Fixes

**JobManagerOptions.cs Location**:
- **Wrong**: `src/S7Tools/Core/Models/Jobs/JobManagerOptions.cs` (UI project)
- **Correct**: `src/S7Tools.Core/Models/Jobs/JobManagerOptions.cs` (Core project)
- Fixed by moving file to proper Clean Architecture location
- Added XML documentation to eliminate compiler warnings

## Build Status

✅ **Final Build**: 0 errors, 0 warnings
✅ **All Tests**: Passing
✅ **Clean Architecture**: Maintained and improved

## Documentation Updated

Files updated to reflect reorganization:
- ✅ `README.md` - Project structure section
- ✅ `.github/copilot-instructions.md` - Namespace conventions and file locations
- ✅ `AGENTS.md` - Folder structure and namespace patterns
- ✅ `.copilot-tracking/memory-bank/systemPatterns.md` - Architecture foundation
- ✅ `PATTERNS_REFERENCE.md` - Clean Architecture pattern examples

## Benefits

1. **Better Organization**: Files grouped by functional purpose, not technical layer
2. **Easier Navigation**: Developers can quickly find related ViewModels/Views
3. **Scalability**: Clear pattern for adding new features
4. **Discoverability**: Category names indicate file purpose
5. **Maintainability**: Related files stay together
6. **Clean Architecture Compliance**: Proper separation maintained
7. **ViewLocator Integration**: Automatic View resolution continues to work

## Migration Notes

### For Future Development

When adding new ViewModels/Views:

1. **Identify Category**: Determine which functional category (Pages, Jobs, Settings, etc.)
2. **Create in Correct Folder**: Place in `ViewModels/{Category}` and `Views/{Category}`
3. **Use Correct Namespace**: `S7Tools.ViewModels.{Category}` and `S7Tools.Views.{Category}`
4. **XAML Namespace**: Use category-specific xmlns declarations
5. **ViewLocator**: Will automatically resolve (no manual registration needed)

### Example: Adding New Page

```csharp
// ViewModel
namespace S7Tools.ViewModels.Pages;
public class MyNewPageViewModel : ViewModelBase { }

// View C#
namespace S7Tools.Views.Pages;
public partial class MyNewPageView : UserControl { }

// View XAML
<UserControl xmlns:vm="using:S7Tools.ViewModels.Pages"
             x:Class="S7Tools.Views.Pages.MyNewPageView"
             x:DataType="vm:MyNewPageViewModel">
```

## Git History

All file moves performed using `git mv` to preserve history:
- Branch: `feature/reorganize-views-viewmodels`
- Base: `SpecKitNew`
- Commits: Atomic commits for file moves, namespace updates, XAML fixes

## Verification Checklist

- [x] All files moved successfully
- [x] All C# namespaces updated
- [x] All XAML x:Class declarations updated
- [x] All XAML xmlns references updated
- [x] All using statements added where needed
- [x] ServiceCollectionExtensions.cs updated
- [x] App.axaml.cs updated
- [x] ViewLocator.cs verified (works with new structure)
- [x] Build successful (0 errors, 0 warnings)
- [x] Documentation updated
- [x] JobManagerOptions.cs moved to correct location
- [x] Clean Architecture boundaries maintained

## Rollback Plan

If needed, rollback via git:
```bash
git checkout SpecKitNew -- src/S7Tools/ViewModels src/S7Tools/Views
git reset HEAD --hard
```

## References

- Planning Document: `.github/VIEW_FOLDER_REORGANIZATION_PLAN.md`
- Memory Bank: `.copilot-tracking/memory-bank/systemPatterns.md`
- Pattern Reference: `PATTERNS_REFERENCE.md`
- Agent Guide: `AGENTS.md`
