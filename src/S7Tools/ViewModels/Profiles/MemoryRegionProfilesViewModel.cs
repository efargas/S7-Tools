using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Dialogs;
using S7Tools.Views.Dialogs;

namespace S7Tools.ViewModels.Profiles;

/// <summary>
/// Represents the MemoryRegionProfilesViewModel.
/// </summary>
public class MemoryRegionProfilesViewModel : ProfileManagementViewModelBase<MemoryMappingProfile>, IDockableViewModel
{
    private readonly IMemoryRegionProfileService _profileService;
    private readonly IUIThreadService _uiThreadService;
    private readonly ILogger<EditMemoryRegionProfileDialogViewModel> _dialogLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryRegionProfilesViewModel"/> class.
    /// </summary>
    public MemoryRegionProfilesViewModel(
        IMemoryRegionProfileService profileService,
        IUnifiedProfileDialogService unifiedDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService,
        IFileDialogService fileDialogService,
        ILogger<MemoryRegionProfilesViewModel> logger,
        ILogger<EditMemoryRegionProfileDialogViewModel> dialogLogger)
        : base(logger, unifiedDialogService, dialogService, uiThreadService, fileDialogService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _uiThreadService = uiThreadService;
        _dialogLogger = dialogLogger ?? throw new ArgumentNullException(nameof(dialogLogger));

        _ = Task.Run(async () =>
        {
            await InitializeAsync();
            RefreshCommand.Execute().Subscribe();
        });
    }

    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "MemoryRegionProfiles";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Memory Regions";
    /// <summary>
    /// Gets or sets the CanClose.
    /// </summary>
    public bool CanClose => true;
    /// <summary>
    /// Gets or sets the CanFloat.
    /// </summary>
    public bool CanFloat => true;

    /// <summary>
    /// Executes the GetProfileManager operation.
    /// </summary>
    protected override IProfileManager<MemoryMappingProfile> GetProfileManager() => _profileService;
    /// <summary>
    /// Executes the GetDefaultProfileName operation.
    /// </summary>
    protected override string GetDefaultProfileName() => "Memory Region Default";
    /// <summary>
    /// Executes the GetProfileTypeName operation.
    /// </summary>
    protected override string GetProfileTypeName() => "Memory Region Profile";
    /// <summary>
    /// Executes the CreateDefaultProfile operation.
    /// </summary>
    protected override MemoryMappingProfile CreateDefaultProfile() => MemoryMappingProfile.CreateDefaultProfile();

    /// <summary>
    /// Executes the ShowCreateDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<MemoryMappingProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        var nameResult = await UnifiedDialogService.ShowNameInputDialogAsync(
            $"Create {GetProfileTypeName()}",
            "Enter a name for the new memory region profile:",
            request.DefaultName ?? GetDefaultProfileName()
        );

        if (!nameResult.IsSuccess)
        {
            return ProfileDialogResult<MemoryMappingProfile>.Cancelled();
        }

        var newProfile = MemoryMappingProfile.CreateUserProfile(nameResult.Result ?? "New Profile");
        var savedProfile = await _profileService.CreateAsync(newProfile);

        return ProfileDialogResult<MemoryMappingProfile>.Success(savedProfile);
    }

    /// <summary>
    /// Executes the ShowEditDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<MemoryMappingProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        if (SelectedProfile == null)
        {
            return ProfileDialogResult<MemoryMappingProfile>.Failure("No profile selected");
        }

        var dialogViewModel = new EditMemoryRegionProfileDialogViewModel(SelectedProfile, _dialogLogger);
        var dialog = new EditMemoryRegionProfileDialog(dialogViewModel);

        bool? dialogResult = await _uiThreadService.InvokeOnUIThreadAsync(async () =>
        {
            Avalonia.Controls.Window? mainWindow = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (mainWindow == null)
            {
                throw new InvalidOperationException("No main window");
            }

            return await dialog.ShowDialog<bool?>(mainWindow);
        }).ConfigureAwait(false);

        if (dialogResult == true)
        {
            var updatedProfile = dialogViewModel.CreateUpdatedProfile();
            if (updatedProfile != null)
            {
                var savedProfile = await _profileService.UpdateAsync(updatedProfile);
                return ProfileDialogResult<MemoryMappingProfile>.Success(savedProfile);
            }
            return ProfileDialogResult<MemoryMappingProfile>.Failure("Failed to modify");
        }

        return ProfileDialogResult<MemoryMappingProfile>.Cancelled();
    }

    /// <summary>
    /// Executes the ShowDuplicateDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        return await UnifiedDialogService.ShowNameInputDialogAsync(
            $"Duplicate {GetProfileTypeName()}",
            $"Enter a name for the duplicated profile:",
            request.SuggestedName ?? "Copy"
        );
    }
}
