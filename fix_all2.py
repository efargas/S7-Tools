import re

# Fix NavigationViewModel
with open('src/S7Tools/ViewModels/Layout/NavigationViewModel.cs', 'r') as f:
    content = f.read()

content = re.sub(
    r'/// <summary>\s*/// Creates a ViewModel for settings configuration.*?\s*private object CreateSettingsConfigViewModel\(\)\s*\{\s*// TODO:.*?return "Settings configuration - needs dedicated ViewModel";\s*\}',
    '',
    content,
    flags=re.DOTALL
)

content = content.replace(
    'MainContent = UIStrings.Navigation_LogViewerComingSoon;',
    'MainContent = new S7Tools.ViewModels.Jobs.JobWizardPlaceholderViewModel(UIStrings.Navigation_LogViewerTitle, UIStrings.Navigation_LogViewerComingSoon);'
)
content = content.replace(
    'DetailContent = UIStrings.Navigation_LogViewerComingSoon;',
    'DetailContent = MainContent;'
)

content = content.replace(
    'MainContent = UIStrings.Navigation_NavigationFailed(ex.Message);',
    'MainContent = new S7Tools.ViewModels.Jobs.JobWizardPlaceholderViewModel(UIStrings.Navigation_ErrorTitle, UIStrings.Navigation_NavigationFailed(ex.Message));'
)
content = content.replace(
    'DetailContent = UIStrings.Navigation_NavigationFailed(ex.Message);',
    'DetailContent = MainContent;'
)

with open('src/S7Tools/ViewModels/Layout/NavigationViewModel.cs', 'w') as f:
    f.write(content)

# Fix JobManager
with open('src/S7Tools/Services/Jobs/JobManager.cs', 'r') as f:
    content = f.read()

old_logic = """                // Use the first selected segment as base configuration
                // TODO: Support multiple segments in JobProfileSet
                MemorySegment firstSegment = selectedSegments.First();
                memoryRegion = new MemoryRegionProfile(firstSegment.StartAddress, (uint)firstSegment.Size);"""

new_logic = """                // The MemoryRegionProfile property is maintained for legacy single-segment compatibility.
                // Modern multi-segment operations use the MemoryMapping property passed below.
                MemorySegment firstSegment = selectedSegments.First();
                memoryRegion = new MemoryRegionProfile(firstSegment.StartAddress ?? "0x20000000", (uint)firstSegment.Size);"""

content = content.replace(old_logic, new_logic)

with open('src/S7Tools/Services/Jobs/JobManager.cs', 'w') as f:
    f.write(content)

# Fix CreateMemoryRegionProfileDialogViewModel
with open('src/S7Tools/ViewModels/Dialogs/CreateMemoryRegionProfileDialogViewModel.cs', 'r') as f:
    content = f.read()

if 'using S7Tools.Services.Interfaces;' not in content:
    content = content.replace(
        'using S7Tools.ViewModels.Base;',
        'using S7Tools.Services.Interfaces;\nusing S7Tools.ViewModels.Base;'
    )

if 'private readonly IDialogService? _dialogService;' not in content:
    content = content.replace(
        'private readonly ILogger<CreateMemoryRegionProfileDialogViewModel> _logger;',
        'private readonly ILogger<CreateMemoryRegionProfileDialogViewModel> _logger;\n    private readonly IDialogService? _dialogService;'
    )

if 'IDialogService? dialogService' not in content:
    content = content.replace(
        'public CreateMemoryRegionProfileDialogViewModel(ILogger<CreateMemoryRegionProfileDialogViewModel> logger)',
        'public CreateMemoryRegionProfileDialogViewModel(\n        ILogger<CreateMemoryRegionProfileDialogViewModel> logger,\n        IDialogService? dialogService = null)'
    )

if '_dialogService = dialogService;' not in content:
    content = content.replace(
        '_logger = logger ?? throw new ArgumentNullException(nameof(logger));',
        '_logger = logger ?? throw new ArgumentNullException(nameof(logger));\n        _dialogService = dialogService;'
    )

old_method = """    private void ExecuteEditSegment()
    {
        try
        {
            if (SelectedSegment != null)
            {
                // TODO: Open segment edit dialog when available
                _logger.LogDebug("Edit segment requested for: {SegmentName}", SelectedSegment.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing custom segment");
        }
    }"""

new_method = """    private async void ExecuteEditSegment()
    {
        try
        {
            if (SelectedSegment != null && _dialogService != null)
            {
                _logger.LogDebug("Edit segment requested for: {SegmentName}", SelectedSegment.Name);

                // For now, use a simple input dialog to change the start address as a placeholder
                // for a full segment edit dialog
                var result = await _dialogService.ShowInputAsync(
                    $"Edit Segment: {SelectedSegment.Name}",
                    "Enter new start address (hex):",
                    SelectedSegment.StartAddress ?? "0x00000000",
                    "0x00000000");

                if (!result.IsCancelled && !string.IsNullOrWhiteSpace(result.Value))
                {
                    SelectedSegment.StartAddress = result.Value;

                    // Force a UI refresh of the segment
                    int index = CustomSegments.IndexOf(SelectedSegment);
                    if (index >= 0)
                    {
                        var temp = SelectedSegment;
                        CustomSegments.RemoveAt(index);
                        CustomSegments.Insert(index, temp);
                        SelectedSegment = temp;
                    }
                }
            }
            else if (_dialogService == null)
            {
                _logger.LogWarning("IDialogService is not available to edit segment");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error editing custom segment");
        }
    }"""

content = content.replace(old_method, new_method)

with open('src/S7Tools/ViewModels/Dialogs/CreateMemoryRegionProfileDialogViewModel.cs', 'w') as f:
    f.write(content)
