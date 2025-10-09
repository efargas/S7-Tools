# Localization and Resource Management Guide

## Overview

S7Tools implements a comprehensive localization system to support internationalization and maintainable string management. This guide explains how to use the existing resource infrastructure.

## Architecture

The localization system consists of several components:

### Core Components

1. **IResourceManager** (`S7Tools.Core.Resources`) - Core interface for resource management
2. **InMemoryResourceManager** (`S7Tools.Core.Resources`) - In-memory implementation for testing/development
3. **ResourceManager** (`S7Tools.Resources`) - Full-featured implementation with caching
4. **ILocalizationService** (`S7Tools.Core.Services.Interfaces`) - High-level localization service interface  
5. **LocalizationService** (`S7Tools.Services`) - Service implementation with culture management

### Resource Files

- **UIStrings.resx** - Main resource file containing all UI strings
- **UIStrings.cs** - Strongly-typed accessor class for resources

## Usage Patterns

### 1. Using UIStrings in ViewModels

**❌ AVOID: Hardcoded Strings**
```csharp
var result = await _dialogService.ShowConfirmationAsync(
    "Exit Application", 
    "Are you sure you want to exit?");
```

**✅ PREFERRED: Using UIStrings**
```csharp
var result = await _dialogService.ShowConfirmationAsync(
    UIStrings.DialogExitTitle, 
    UIStrings.DialogExitMessage);
```

### 2. Using ILocalizationService

Inject the localization service in your ViewModel:

```csharp
public class MyViewModel : ViewModelBase
{
    private readonly ILocalizationService _localization;
    
    public MyViewModel(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }
    
    private async Task ShowMessageAsync()
    {
        var title = _localization.GetString("DialogTitle");
        var message = _localization.GetString("DialogMessage");
        
        await _dialogService.ShowErrorAsync(title, message);
    }
}
```

### 3. Formatted Strings

For strings with placeholders:

**Resource Entry:**
```xml
<data name="ExportSuccess" xml:space="preserve">
  <value>Successfully exported {0} entries to {1}</value>
</data>
```

**Usage:**
```csharp
var message = _localization.GetString("ExportSuccess", count, fileName);
// OR using string interpolation with resources:
var message = string.Format(UIStrings.ExportSuccess, count, fileName);
```

### 4. Adding New Resource Strings

#### Step 1: Add to UIStrings.resx

Edit `src/S7Tools/Resources/Strings/UIStrings.resx`:

```xml
<data name="YourNewKey" xml:space="preserve">
  <value>Your localized text</value>
  <comment>Description of where this is used</comment>
</data>
```

#### Step 2: Add Property to UIStrings.cs

Edit `src/S7Tools/Resources/UIStrings.cs`:

```csharp
/// <summary>
/// Gets the your new text description.
/// </summary>
public static string YourNewKey => ResourceManager.GetString("YourNewKey") ?? "Fallback text";
```

#### Step 3: Use in Your Code

```csharp
var text = UIStrings.YourNewKey;
```

## Common Patterns

### Dialog Messages

```csharp
// Confirmation dialogs
var result = await _dialogService.ShowConfirmationAsync(
    UIStrings.DialogConfirmationTitle,
    UIStrings.ClearLogsConfirmation);

// Error dialogs
await _dialogService.ShowErrorAsync(
    UIStrings.DialogErrorTitle,
    UIStrings.ErrorConnection);

// Success messages
await _dialogService.ShowErrorAsync(
    UIStrings.DialogInformationTitle,
    _localization.GetString("OperationSuccess", operationName));
```

### Status Messages

```csharp
StatusMessage = UIStrings.StatusLoading;
// ... perform operation ...
StatusMessage = UIStrings.StatusReady;
```

### Button Labels

```csharp
<Button Content="{x:Static resources:UIStrings.DialogOK}" />
<Button Content="{x:Static resources:UIStrings.DialogCancel}" />
```

### Menu Items

```csharp
<MenuItem Header="{x:Static resources:UIStrings.MenuFile}">
    <MenuItem Header="{x:Static resources:UIStrings.MenuNewFile}" />
    <MenuItem Header="{x:Static resources:UIStrings.MenuSave}" />
</MenuItem>
```

## Culture Management

### Changing Culture at Runtime

```csharp
public class SettingsViewModel : ViewModelBase
{
    private readonly ILocalizationService _localization;
    
    public async Task SetLanguageAsync(string cultureName)
    {
        if (_localization.SetCulture(cultureName))
        {
            // Culture changed successfully
            // Notify UI to refresh
            await RefreshUIAsync();
        }
    }
    
    public IReadOnlyList<CultureInfo> AvailableLanguages => 
        _localization.AvailableCultures;
}
```

### Getting Current Culture

```csharp
var currentCulture = _localization.CurrentCulture;
var currentUILanguage = currentCulture.DisplayName;
```

## Best Practices

### DO

✅ **Use UIStrings for all user-visible text**
```csharp
Title = UIStrings.ApplicationTitle;
```

✅ **Add XML comments to resource properties**
```csharp
/// <summary>
/// Gets the confirmation message for clearing logs.
/// </summary>
public static string ClearLogsConfirmation => ...
```

✅ **Provide fallback values**
```csharp
public static string MyText => ResourceManager.GetString("MyText") ?? "Fallback";
```

✅ **Group related strings with prefixes**
```
Dialog_ConfirmationTitle
Dialog_ErrorTitle
Menu_File
Menu_Edit
Status_Ready
Status_Loading
```

### DON'T

❌ **Don't hardcode strings in ViewModels**
```csharp
StatusMessage = "Loading..."; // BAD
```

❌ **Don't skip resource comments**
```xml
<data name="SomeText" xml:space="preserve">
  <value>Text</value>
  <!-- Missing comment explaining usage -->
</data>
```

❌ **Don't duplicate strings**
```csharp
// BAD: Multiple places with same text
var msg1 = "Are you sure?";
var msg2 = "Are you sure?";

// GOOD: Reuse resource
var msg1 = UIStrings.ConfirmationPrompt;
var msg2 = UIStrings.ConfirmationPrompt;
```

## Migration Strategy

### Phase 1: Identify Hardcoded Strings

Search codebase for hardcoded strings:
```bash
grep -r "\"[A-Z]" src --include="*.cs" | grep -v "//" | grep -v "<!--"
```

### Phase 2: Add Missing Resources

For each hardcoded string:
1. Add entry to UIStrings.resx
2. Add property to UIStrings.cs
3. Add comment explaining usage

### Phase 3: Refactor Code

Replace hardcoded strings with resource references:

**Before:**
```csharp
var result = await _dialogService.ShowConfirmationAsync(
    "Delete Profile",
    "Are you sure you want to delete this profile?");
```

**After:**
```csharp
var result = await _dialogService.ShowConfirmationAsync(
    UIStrings.Dialog_DeleteProfile_Title,
    UIStrings.Dialog_DeleteProfile_Message);
```

### Phase 4: Verify

1. Build solution to ensure no compilation errors
2. Run application to verify strings display correctly  
3. Test language switching if implemented
4. Run automated tests

## Example: Complete Migration

### Original Code (Hardcoded)

```csharp
public class LogViewerViewModel : ViewModelBase
{
    private async Task ClearLogsAsync()
    {
        var result = await _dialogService.ShowConfirmationAsync(
            "Clear Logs",
            "Are you sure you want to clear all log entries? This action cannot be undone.");
        
        if (result)
        {
            _logDataStore.Clear();
            StatusMessage = "Logs cleared successfully";
        }
    }
}
```

### Step 1: Add Resources

In `UIStrings.resx`:
```xml
<data name="LogViewer_ClearLogsTitle" xml:space="preserve">
  <value>Clear Logs</value>
  <comment>Title for clear logs confirmation dialog</comment>
</data>
<data name="LogViewer_ClearLogsMessage" xml:space="preserve">
  <value>Are you sure you want to clear all log entries? This action cannot be undone.</value>
  <comment>Message for clear logs confirmation dialog</comment>
</data>
<data name="LogViewer_ClearedSuccess" xml:space="preserve">
  <value>Logs cleared successfully</value>
  <comment>Status message after logs are cleared</comment>
</data>
```

### Step 2: Add Properties

In `UIStrings.cs`:
```csharp
/// <summary>
/// Gets the title for the clear logs confirmation dialog.
/// </summary>
public static string LogViewer_ClearLogsTitle => 
    ResourceManager.GetString("LogViewer_ClearLogsTitle") ?? "Clear Logs";

/// <summary>
/// Gets the message for the clear logs confirmation dialog.
/// </summary>
public static string LogViewer_ClearLogsMessage => 
    ResourceManager.GetString("LogViewer_ClearLogsMessage") ?? 
    "Are you sure you want to clear all log entries? This action cannot be undone.";

/// <summary>
/// Gets the success message after logs are cleared.
/// </summary>
public static string LogViewer_ClearedSuccess => 
    ResourceManager.GetString("LogViewer_ClearedSuccess") ?? "Logs cleared successfully";
```

### Step 3: Refactor Code

```csharp
public class LogViewerViewModel : ViewModelBase
{
    private async Task ClearLogsAsync()
    {
        var result = await _dialogService.ShowConfirmationAsync(
            UIStrings.LogViewer_ClearLogsTitle,
            UIStrings.LogViewer_ClearLogsMessage);
        
        if (result)
        {
            _logDataStore.Clear();
            StatusMessage = UIStrings.LogViewer_ClearedSuccess;
        }
    }
}
```

## Testing Localization

### Unit Tests

```csharp
[Fact]
public void LocalizationService_GetString_ReturnsExpectedValue()
{
    // Arrange
    var localization = new LocalizationService();
    
    // Act
    var result = localization.GetString("ApplicationTitle");
    
    // Assert
    Assert.NotNull(result);
    Assert.NotEqual("ApplicationTitle", result); // Should not return key
}

[Fact]
public void UIStrings_ApplicationTitle_ReturnsNonEmptyString()
{
    // Arrange & Act
    var title = UIStrings.ApplicationTitle;
    
    // Assert
    Assert.NotNull(title);
    Assert.NotEmpty(title);
}
```

### Manual Testing

1. Run application
2. Navigate to all views
3. Verify all text displays correctly
4. Check for any remaining hardcoded strings
5. Test with different cultures if supported

## Future Enhancements

### Multi-Language Support

To add additional languages:

1. Create culture-specific resource files:
   - `UIStrings.es-ES.resx` (Spanish)
   - `UIStrings.de-DE.resx` (German)
   - etc.

2. Add translations for each key in the new files

3. Update `LocalizationService.GetAvailableCultures()` if needed

4. Test language switching functionality

### Resource Validation

Consider adding build-time validation:

```csharp
[Fact]
public void AllUIStringsProperties_HaveMatchingResourceEntries()
{
    var properties = typeof(UIStrings).GetProperties(BindingFlags.Public | BindingFlags.Static);
    var resourceManager = UIStrings.ResourceManager;
    
    foreach (var property in properties)
    {
        var key = property.Name;
        var value = resourceManager.GetString(key);
        
        Assert.NotNull(value);
        Assert.NotEmpty(value);
    }
}
```

## Support

For questions or issues with localization:
1. Check this guide first
2. Review existing resource usage patterns in the codebase
3. Consult the team lead or architect
4. Update this guide with new patterns as they emerge

---

**Last Updated**: 2025
**Maintained By**: S7Tools Development Team
