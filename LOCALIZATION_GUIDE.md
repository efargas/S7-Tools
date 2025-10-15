# Localization and Resource Management Guide (S7Tools)

Last Updated: 2025-10-15
Maintained By: S7Tools Development Team

Overview

S7Tools ships with two complementary localization mechanisms:
- ResX resources (compiled) for strongly-typed, satellite-assembly friendly strings
- An abstract IResourceManager for flexible lookups and test-time overrides

Recent refactors introduced a custom UIStrings wrapper (S7Tools.Resources.UIStrings) with safe fallbacks and an InMemoryResourceManager default DI registration for development. This guide aligns documentation with the current implementation and defines a clear migration path to consistent resource usage.

Architecture

Core Components

1) IResourceManager (S7Tools.Core.Resources)
- Abstraction over resource retrieval with culture support
- Implementations: InMemoryResourceManager (default in DI), production ResourceManager (S7Tools.Resources)

2) InMemoryResourceManager (S7Tools.Core.Resources)
- Simple in-memory implementation used by default for development and tests
- Allows injecting ad-hoc strings during unit testing

3) S7Tools.Resources.ResourceManager (S7Tools/Resources/ResourceManager.cs)
- Full-featured runtime implementation with caching and pluggable ResX resource managers
- Registers the compiled ResX "S7Tools.Resources.Strings.UIStrings" on startup
- Note: This class currently shares the name ResourceManager with another class in S7Tools.Core.Resources. See Naming Conflicts below

4) UIStrings.resx + UIStrings.Designer.cs (S7Tools/Resources/Strings)
- Compiled resource file and generated strongly typed accessor (S7Tools.Resources.Strings.UIStrings)
- Keys currently use underscore grouping (e.g., ActivityBar_Explorer, Menu_File)

5) S7Tools.Resources.UIStrings (S7Tools/Resources/UIStrings.cs)
- Project-specific strongly-typed wrapper over IResourceManager with safe defaults
- Exposes properties for common UI text with hardcoded fallbacks
- Initialized in App.Initialize via DI-resolved IResourceManager

6) ILocalizationService + LocalizationService (S7Tools/Services)
- High-level service for culture switching and lookups using the compiled ResX (S7Tools.Resources.Strings.UIStrings.ResourceManager)
- Emits CultureChanged events; sets thread and default cultures

Current Wiring (as of 2025-10-15)

- DI registers IResourceManager -> S7Tools.Core.Resources.InMemoryResourceManager
- App.Initialize sets S7Tools.Resources.UIStrings.ResourceManager = IResourceManager (the in-memory instance)
- LocalizationService uses the compiled ResX ResourceManager (S7Tools.Resources.Strings.UIStrings.ResourceManager) directly
- Result: S7Tools.Resources.UIStrings retrieves values from IResourceManager, which by default returns fallback strings unless values are populated at runtime; LocalizationService retrieves from ResX

Implications

- Two sources of truth can diverge:
  - UIStrings.cs property keys mostly use CamelCase format (e.g., ActivityBarExplorer, MenuFile)
  - UIStrings.resx keys use underscore grouping (e.g., ActivityBar_Explorer, Menu_File)
- With the default InMemoryResourceManager, UIStrings.cs returns its coded fallbacks, while LocalizationService returns ResX values. This is acceptable in development but not ideal for production consistency.

Recommended Target State

- Use S7Tools.Resources.ResourceManager (production) as the IResourceManager DI registration so that UIStrings.cs reads from ResX resources by default
- Establish a single naming policy for keys and align UIStrings.cs property names with ResX keys
- Prefer underscore grouping for readability and discoverability in ResX (e.g., ActivityBar_Explorer, Dialog_OK)

How to Switch to Production ResourceManager

1) Update DI registration (ServiceCollectionExtensions)
- Replace:
  services.TryAddSingleton<IResourceManager, S7Tools.Core.Resources.InMemoryResourceManager>();
- With:
  services.TryAddSingleton<IResourceManager, S7Tools.Resources.ResourceManager>();

2) Verify App.Initialize
- App.Initialize already sets UIStrings.ResourceManager = serviceProvider.GetRequiredService<IResourceManager>();
- No additional change needed

3) Build and run
- UIStrings.cs will now resolve through S7Tools.Resources.ResourceManager which internally registers the ResX resource manager for S7Tools.Resources.Strings.UIStrings

Key Naming Policy and Migration

Policy
- Adopt underscore-grouped keys in ResX for clarity: Group_Subgroup_Name
  - Examples: ActivityBar_Explorer, Panel_LogViewer, Dialog_OK, Status_Ready
- UIStrings.cs property names should map 1-to-1 to resource keys
  - Example property: public static string ActivityBar_Explorer => ResourceManager.GetString("ActivityBar_Explorer") ?? "Explorer";

Current Mismatch
- UIStrings.cs includes several properties with CamelCase keys (e.g., ActivityBarExplorer) while UIStrings.resx uses underscores (ActivityBar_Explorer)
- Until code is normalized, add ResX aliases to maintain compatibility:
  - For each UIStrings.cs property using CamelCase key, add a duplicate key in UIStrings.resx matching that CamelCase name that points to the same text

Normalization Plan
- Phase A (Safe, Non-breaking): Add alias entries in UIStrings.resx for CamelCase keys currently requested by UIStrings.cs
- Phase B (Code Align): Update S7Tools/Resources/UIStrings.cs to consume underscore-grouped keys and optionally deprecate CamelCase properties
- Phase C (Cleanup): Remove alias keys from ResX after all call sites are aligned and verified

Usage Patterns

1) Using S7Tools.Resources.UIStrings in ViewModels (preferred for UI text)

// Avoid hardcoded strings
var result = await _dialogService.ShowConfirmationAsync(
    UIStrings.DialogConfirmationTitle,
    UIStrings.Confirm_ClearLogs);

// Status updates
StatusMessage = UIStrings.Status_Ready;

// Composite messages
var msg = string.Format(UIStrings.LogViewer_ExportSuccess, count, format);

2) Using ILocalizationService for raw key lookups or culture switching

public class MyViewModel
{
    private readonly ILocalizationService _localization;

    public MyViewModel(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    private async Task ShowErrorAsync()
    {
        var title = UIStrings.DialogErrorTitle; // strongly-typed property
        var message = _localization.GetString("Error_ConnectionFailed"); // direct key
        await _dialogService.ShowErrorAsync(title, message);
    }
}

Formatted Strings

ResX Entry Example:
<data name="LogViewer_ExportSuccess" xml:space="preserve">
  <value>Successfully exported {0} log entries to {1} format.</value>
  <comment>Message after exporting logs; args: count, format</comment>
</data>

Usage:
var message = string.Format(UIStrings.LogViewer_ExportSuccess, count, format);
// or via ILocalizationService
var message = _localization.GetString("LogViewer_ExportSuccess", count, format);

Adding New Resource Strings

Step 1: Add to UIStrings.resx (S7Tools/Resources/Strings/UIStrings.resx)

<data name="Dialog_DeleteProfile_Title" xml:space="preserve">
  <value>Delete Profile</value>
  <comment>Title for delete profile confirmation dialog</comment>
</data>
<data name="Dialog_DeleteProfile_Message" xml:space="preserve">
  <value>Are you sure you want to delete this profile?</value>
  <comment>Message for delete profile confirmation dialog</comment>
</data>

Step 2: Expose via S7Tools.Resources.UIStrings
- Add corresponding properties in S7Tools/Resources/UIStrings.cs using the same key names

/// <summary>
/// Gets the delete profile dialog title.
/// </summary>
public static string Dialog_DeleteProfile_Title => ResourceManager.GetString("Dialog_DeleteProfile_Title") ?? "Delete Profile";

/// <summary>
/// Gets the delete profile dialog message.
/// </summary>
public static string Dialog_DeleteProfile_Message => ResourceManager.GetString("Dialog_DeleteProfile_Message") ?? "Are you sure you want to delete this profile?";

Step 3: Use in code
var confirmed = await _dialogService.ShowConfirmationAsync(
    UIStrings.Dialog_DeleteProfile_Title,
    UIStrings.Dialog_DeleteProfile_Message);

Common Patterns

Dialog Messages

await _dialogService.ShowErrorAsync(
    UIStrings.DialogErrorTitle,
    _localization.GetString("Error_InvalidConfiguration"));

Status Messages

StatusMessage = UIStrings.Status_Loading;
// ... work ...
StatusMessage = UIStrings.Status_Ready;

XAML Bindings (Avalonia)

<Button Content="{x:Static resources:UIStrings.DialogOK}" />
<Button Content="{x:Static resources:UIStrings.DialogCancel}" />

Menu Items

<MenuItem Header="{x:Static resources:UIStrings.Menu_File}">
    <MenuItem Header="{x:Static resources:UIStrings.Action_Open}" />
    <MenuItem Header="{x:Static resources:UIStrings.Action_Save}" />
</MenuItem>

Culture Management

Changing Culture at Runtime

public class SettingsViewModel : ViewModelBase
{
    private readonly ILocalizationService _localization;

    public async Task SetLanguageAsync(string cultureName)
    {
        if (_localization.SetCulture(cultureName))
        {
            await RefreshUIAsync();
        }
    }

    public IReadOnlyList<CultureInfo> AvailableLanguages => _localization.AvailableCultures;
}

Getting Current Culture

var currentCulture = _localization.CurrentUICulture;
var currentLanguage = currentCulture.DisplayName;

Best Practices

DO
- Use S7Tools.Resources.UIStrings for user-visible text in UI/ViewModels
- Use ILocalizationService for culture switching and direct key lookups
- Add XML summaries to new UIStrings properties
- Provide fallback values in UIStrings.cs properties
- Group keys with prefixes (Dialog_, Menu_, Status_, LogViewer_, etc.)

DON'T
- Do not hardcode user-visible strings in ViewModels or services
- Do not introduce new key naming styles; use underscore grouping
- Do not bypass ILocalizationService for culture changes

Migration Strategy (Hardcoded to Resources)

Phase 1: Identify Hardcoded Strings
Suggested search:
- grep -R "\"[A-Z][a-zA-Z ]\{3,\}\"" src --include="*.cs" | grep -v "//"

Phase 2: Add Missing Resources
- Create keys in UIStrings.resx per naming policy
- Add properties in S7Tools/Resources/UIStrings.cs with fallbacks
- Add comments describing usage

Phase 3: Refactor Code
Before:
var result = await _dialogService.ShowConfirmationAsync("Clear Logs", "Are you sure?");

After:
var result = await _dialogService.ShowConfirmationAsync(
    UIStrings.LogViewer_ClearLogsTitle,
    UIStrings.LogViewer_ClearLogsMessage);

Phase 4: Verify
- Build solution
- Run app and toggle language
- Validate strings in all views
- Ensure no remaining hardcoded UI text

Naming Conflicts and Warnings (ResourceManager)

- There are two classes named ResourceManager:
  1) S7Tools.Core.Resources.ResourceManager (delegates to InMemoryResourceManager)
  2) S7Tools.Resources.ResourceManager (production, caches and registers ResX managers)
- This may lead to confusion and CS0436 warnings in some IDEs/builds. Plan to rename the production class to S7ToolsResourceManager and update DI registrations and usages accordingly

Verification & Tests

Unit Tests

[Fact]
public void UIStrings_ApplicationTitle_ProvidesFallback()
{
    // When DI provides InMemoryResourceManager without values,
    // UIStrings should return fallback text
    Assert.False(string.IsNullOrEmpty(S7Tools.Resources.UIStrings.ApplicationTitle));
}

[Fact]
public void LocalizationService_GetString_ResolvesFromResx()
{
    var svc = new S7Tools.Services.LocalizationService();
    var value = svc.GetString("ApplicationTitle");
    Assert.False(string.IsNullOrEmpty(value));
}

Manual Testing

1. Start the app
2. Navigate across views; verify text comes from UIStrings
3. Change culture using settings; verify culture switches
4. Run through dialogs (confirmation/error/input) and verify localized text

Future Enhancements

Multi-Language Support
- Add culture-specific resource files: UIStrings.es-ES.resx, UIStrings.de-DE.resx, etc.
- Populate translations and rebuild
- Expand AvailableCultures in LocalizationService or detect satellite assemblies dynamically

Resource Validation
- Add a test that reflects over S7Tools.Resources.UIStrings properties and asserts all keys exist in UIStrings.resx
- During migration keep fallbacks; once complete, enforce presence

Appendix: Key Mapping (Examples)

- ActivityBarExplorer (CamelCase in code) -> ActivityBar_Explorer (ResX)
- MenuFile -> Menu_File
- PanelLogViewer -> Panel_LogViewer
- DialogOK -> Dialog_OK

During Phase A, ensure UIStrings.resx contains both forms if needed to avoid regressions.
