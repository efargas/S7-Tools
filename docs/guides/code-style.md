---
title: "Code Style Guide"
version: "1.0.0"
created: "2025-11-10"
last-updated: "2025-11-10"
status: "current"
tags: ["guide", "code-style", "formatting", "conventions"]
related:
  - docs/guides/development-workflow.md
  - docs/guides/testing-guide.md
  - docs/architecture/overview.md
---

# Code Style Guide

This guide defines the coding standards and style conventions for S7Tools. All code must follow these rules and be formatted with `dotnet format` before commit.

## EditorConfig

Code style is enforced via `.editorconfig` at the repository root. The configuration is automatically applied by Visual Studio Code, Visual Studio, and Rider.

### Running Code Formatter

```bash
# REQUIRED before every commit
dotnet format src/S7Tools.sln

# Check formatting without making changes
dotnet format src/S7Tools.sln --verify-no-changes
```

## General Conventions

### Indentation

| Language | Indentation | Rule |
|----------|-------------|------|
| **C#** | 4 spaces | No tabs |
| **XAML** | 2 spaces | No tabs |
| **JSON** | 2 spaces | No tabs |
| **Markdown** | 2-4 spaces (lists) | Consistent within file |

### Line Length

- **Soft limit**: 120 characters
- **Hard limit**: 140 characters (enforced by EditorConfig)
- Break long lines at logical points (parameters, operators)

### File Organization

```csharp
// 1. Namespace (no file-scoped namespaces for clarity)
namespace S7Tools.ViewModels.Pages;

// 2. Using statements (sorted, system first)
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReactiveUI;

// 3. Type definition
public class HomeViewModel : ReactiveObject
{
    // 4. Fields (private, prefixed with _)
    private readonly ILogger<HomeViewModel> _logger;
    private string _title = string.Empty;

    // 5. Constructors
    public HomeViewModel(ILogger<HomeViewModel> logger)
    {
        _logger = logger;
    }

    // 6. Properties (public first, then private)
    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    // 7. Methods (public first, then private)
    public async Task LoadAsync()
    {
        // Implementation
    }

    private void UpdateState()
    {
        // Implementation
    }
}
```

## Naming Conventions

### Types

```csharp
// PascalCase for classes, interfaces, structs, enums
public class SerialProfileService { }
public interface IProfileService { }
public struct PortConfiguration { }
public enum ProfileStatus { }

// Interfaces prefixed with 'I'
public interface IActivityBarService { }

// Async methods suffixed with 'Async'
public async Task<Profile> LoadProfileAsync() { }
```

### Members

```csharp
// PascalCase for public members
public string ProfileName { get; set; }
public void SaveProfile() { }

// camelCase for private fields with _ prefix
private readonly ILogger _logger;
private string _currentStatus;

// camelCase for parameters
public void UpdateProfile(string profileName, int baudRate) { }

// camelCase for local variables
var profileList = new List<Profile>();
```

### Constants

```csharp
// PascalCase for public constants
public const int DefaultBaudRate = 9600;

// UPPER_CASE for private constants (optional)
private const int MAX_RETRIES = 3;

// Prefer static readonly for complex constants
public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
```

### Special Patterns

```csharp
// ViewModels: Suffix with 'ViewModel'
public class ProfileManagementViewModel { }

// Views: Suffix with 'View' or 'Control'
public class ProfileManagementView { }
public class SerialPortDiscoveryControl { }

// Services: Suffix with 'Service'
public class SerialProfileService { }

// Exceptions: Suffix with 'Exception'
public class ProfileNotFoundException : Exception { }
```

## C# Style Rules

### Braces

```csharp
// ✅ Always use braces, even for single statements
if (condition)
{
    DoSomething();
}

// ❌ Never omit braces
if (condition)
    DoSomething();  // NO!
```

### var Usage

```csharp
// ✅ Use var when type is obvious
var profiles = new List<SerialProfile>();
var service = Substitute.For<IProfileService>();

// ✅ Use explicit type when not obvious
ILogger<MyClass> logger = GetLogger();
int count = GetCount();

// ❌ Don't use var for primitives when value is unclear
var x = GetValue();  // What type is x?
```

### String Handling

```csharp
// ✅ Use string interpolation
var message = $"Profile {profile.Name} saved successfully";

// ✅ Use verbatim strings for paths
var path = @"C:\Users\Public\Documents";

// ✅ Use raw string literals for multiline (C# 11+)
var json = """
    {
        "name": "test"
    }
    """;

// ❌ Avoid concatenation
var message = "Profile " + profile.Name + " saved";  // NO!
```

### Null Handling

```csharp
// ✅ Use null-conditional operator
var length = profile?.Name?.Length ?? 0;

// ✅ Use null-coalescing operator
var name = profile?.Name ?? "Unknown";

// ✅ Use ArgumentNullException.ThrowIfNull (C# 11+)
ArgumentNullException.ThrowIfNull(profile);

// ✅ Use nullable reference types
public class ProfileService
{
    private readonly ILogger<ProfileService> _logger;  // Non-nullable
    private Profile? _currentProfile;  // Nullable
}
```

### LINQ

```csharp
// ✅ Use method syntax for simple queries
var active = profiles.Where(p => p.IsActive).ToList();

// ✅ Use query syntax for complex queries
var result = from p in profiles
             where p.IsActive
             orderby p.Name
             select new { p.Name, p.BaudRate };

// ✅ Use async LINQ when possible
var profiles = await context.Profiles
    .Where(p => p.IsActive)
    .ToListAsync();
```

### Async/Await

```csharp
// ✅ Always use async/await, never .Result or .Wait()
public async Task<Profile> LoadProfileAsync(Guid id)
{
    return await _repository.GetByIdAsync(id).ConfigureAwait(false);
}

// ✅ Use ConfigureAwait(false) in library code
await SomeMethodAsync().ConfigureAwait(false);

// ✅ Omit ConfigureAwait in UI code (ViewModels)
await LoadDataAsync();  // UI context needed

// ❌ Never block on async
var profile = LoadProfileAsync(id).Result;  // DEADLOCK RISK!
```

## XAML Style Rules

### Indentation

```xaml
<!-- 2-space indentation -->
<Window>
  <Grid>
    <StackPanel>
      <TextBlock Text="Hello" />
    </StackPanel>
  </Grid>
</Window>
```

### Property Formatting

```xaml
<!-- Simple properties on one line -->
<TextBlock Text="Profile Name" FontSize="14" />

<!-- Complex properties on multiple lines -->
<Button
  Content="Save"
  Command="{Binding SaveCommand}"
  IsEnabled="{Binding CanSave}">
  <Button.Style>
    <Style TargetType="Button">
      <Setter Property="Background" Value="Blue" />
    </Style>
  </Button.Style>
</Button>
```

### Resource Keys

```xaml
<!-- PascalCase for resource keys -->
<SolidColorBrush x:Key="PrimaryBrush" Color="#007ACC" />
<Style x:Key="HeaderTextBlockStyle" TargetType="TextBlock">
  <Setter Property="FontSize" Value="18" />
</Style>
```

## ReactiveUI Patterns

### Property Declaration

```csharp
// ✅ Backing field + RaiseAndSetIfChanged
private string _profileName = string.Empty;
public string ProfileName
{
    get => _profileName;
    set => this.RaiseAndSetIfChanged(ref _profileName, value);
}

// ❌ Don't use auto-properties for bindable properties
public string ProfileName { get; set; }  // NO - won't notify!
```

### Command Declaration

```csharp
// ✅ Declare commands as properties
public ReactiveCommand<Unit, Unit> SaveCommand { get; }

// ✅ Initialize in constructor with CanExecute
public MyViewModel()
{
    var canSave = this.WhenAnyValue(x => x.IsDirty, x => x.IsValid,
        (dirty, valid) => dirty && valid);

    SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync, canSave);
}

private async Task SaveAsync()
{
    // Implementation
}
```

## Documentation Comments

### Public APIs

```csharp
/// <summary>
/// Creates a new serial profile with the specified configuration.
/// </summary>
/// <param name="profile">The profile to create.</param>
/// <returns>The created profile with assigned ID.</returns>
/// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
/// <exception cref="DuplicateProfileNameException">Thrown when profile name already exists.</exception>
public async Task<SerialProfile> CreateAsync(SerialProfile profile)
{
    // Implementation
}
```

### Complex Logic

```csharp
// Explain WHY, not WHAT
// Good: Explains reasoning
// We use semaphore instead of lock to support async operations
// and prevent deadlocks when calling from UI thread
private readonly SemaphoreSlim _semaphore = new(1, 1);

// Bad: States the obvious
// This is a semaphore
private readonly SemaphoreSlim _semaphore = new(1, 1);
```

## Resource Strings

### Localization

```csharp
// ✅ Use resource strings for UI text
Text = UIStrings.ProfileManagement_SaveButton;

// ✅ Use string.Format or interpolation with resources
var message = string.Format(UIStrings.ProfileSaved_Format, profileName);

// ❌ Never hardcode UI strings
Text = "Save Profile";  // NO!
```

### Resource Key Convention

```
Category_Context_Element

Examples:
ProfileManagement_SaveButton
ProfileManagement_DeleteConfirmation_Title
Validation_ProfileName_Required
```

## Exception Handling

### Throwing Exceptions

```csharp
// ✅ Use specific exceptions
throw new ProfileNotFoundException(profileId);
throw new ArgumentNullException(nameof(profile));

// ✅ Include context in message
throw new InvalidOperationException(
    $"Cannot delete profile '{profile.Name}' while it is in use");

// ❌ Don't throw generic exceptions
throw new Exception("Error");  // NO!
```

### Catching Exceptions

```csharp
// ✅ Catch specific exceptions
try
{
    await _service.DeleteAsync(id);
}
catch (ProfileNotFoundException ex)
{
    _logger.LogWarning(ex, "Profile not found: {ProfileId}", id);
    ShowError("Profile not found");
}

// ✅ Always log exceptions
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error deleting profile");
    throw;  // Rethrow to preserve stack trace
}

// ❌ Never swallow exceptions
catch (Exception)
{
    // Ignored
}  // NO!
```

## Logging

### Structured Logging

```csharp
// ✅ Use structured logging with placeholders
_logger.LogInformation("Profile {ProfileName} created with ID {ProfileId}",
    profile.Name, profile.Id);

// ✅ Use appropriate log levels
_logger.LogDebug("Cache hit for profile {ProfileId}", id);
_logger.LogInformation("Profile saved successfully");
_logger.LogWarning("Profile name already exists: {Name}", name);
_logger.LogError(ex, "Failed to save profile");

// ❌ Never use string interpolation in log messages
_logger.LogInformation($"Profile {profile.Name} created");  // NO!
```

## Performance Considerations

### Collections

```csharp
// ✅ Use appropriate collection types
List<T>           // General purpose, ordered
Dictionary<K, V>  // Fast lookup by key
HashSet<T>        // Fast lookup, no duplicates
ObservableCollection<T>  // UI binding

// ✅ Specify capacity when known
var list = new List<Profile>(capacity: 100);

// ✅ Use collection expressions (C# 12+)
int[] numbers = [1, 2, 3, 4, 5];
```

### String Building

```csharp
// ✅ Use StringBuilder for multiple concatenations
var sb = new StringBuilder();
foreach (var profile in profiles)
{
    sb.AppendLine(profile.Name);
}
var result = sb.ToString();

// ❌ Don't concatenate in loops
string result = "";
foreach (var profile in profiles)
{
    result += profile.Name;  // NO - allocates every iteration!
}
```

## Testing Style

### Test Naming

```csharp
// Pattern: MethodName_Scenario_ExpectedBehavior
[Fact]
public async Task CreateAsync_ValidProfile_ReturnsNewProfile()
{
    // Test implementation
}

[Fact]
public async Task DeleteAsync_NonExistentId_ThrowsProfileNotFoundException()
{
    // Test implementation
}
```

### AAA Pattern

```csharp
[Fact]
public async Task LoadAsync_ValidId_LoadsProfile()
{
    // Arrange
    var expectedProfile = new SerialProfile { Id = testId };
    _mockService.GetByIdAsync(testId).Returns(expectedProfile);

    // Act
    var result = await _sut.LoadAsync(testId);

    // Assert
    result.Should().BeEquivalentTo(expectedProfile);
}
```

## Common Anti-Patterns to Avoid

### ❌ Magic Numbers

```csharp
// Bad
if (baudRate == 9600) { }
Thread.Sleep(5000);

// Good
if (baudRate == NetworkConstants.DefaultBaudRate) { }
await Task.Delay(TimeConstants.DefaultTimeout);
```

### ❌ Nested Conditionals

```csharp
// Bad - hard to read
if (profile != null)
{
    if (profile.IsActive)
    {
        if (profile.Name != "")
        {
            // Do something
        }
    }
}

// Good - early returns
if (profile == null) return;
if (!profile.IsActive) return;
if (string.IsNullOrEmpty(profile.Name)) return;
// Do something
```

### ❌ God Objects

```csharp
// Bad - class does too much
public class ProfileManager
{
    public void CreateProfile() { }
    public void DeleteProfile() { }
    public void ExportToFile() { }
    public void ImportFromFile() { }
    public void ValidateProfile() { }
    public void SendToDevice() { }
}

// Good - separated responsibilities
public class ProfileService { }
public class ProfileExporter { }
public class ProfileValidator { }
public class DeviceCommunicator { }
```

## EditorConfig Reference

Key settings from `.editorconfig`:

```ini
# C# files
[*.cs]
indent_size = 4
max_line_length = 140
dotnet_sort_system_directives_first = true
csharp_new_line_before_open_brace = all
csharp_prefer_braces = true:warning

# XAML files
[*.xaml]
indent_size = 2
max_line_length = 140
```

## Pre-Commit Checklist

Before every commit:

```bash
# 1. Format code (MANDATORY)
dotnet format src/S7Tools.sln

# 2. Build without warnings
dotnet build src/S7Tools.sln --configuration Debug

# 3. Run tests
dotnet test src/S7Tools.sln --configuration Debug

# 4. Verify formatting (optional verification)
dotnet format src/S7Tools.sln --verify-no-changes
```

## References

- [Development Workflow](./development-workflow.md)
- [Testing Guide](./testing-guide.md)
- [Architecture Overview](../architecture/overview.md)
- [Pattern Catalog](../patterns/_index.md)

---

*Last Updated*: 2025-11-10

## Related Documentation

- [Index](../INDEX.md)
- [Overview](../architecture/overview.md)
- [Development Workflow](development-workflow.md)
- [Testing Guide](testing-guide.md)
- [_Index](../patterns/_index.md)

---
*This section is auto-generated. Do not edit manually. Last updated: 2025-11-10*
