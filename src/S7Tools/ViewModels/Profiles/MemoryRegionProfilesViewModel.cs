using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models;
using S7Tools.Core.Services.Interfaces;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Dialogs;
using S7Tools.Views.Dialogs;

namespace S7Tools.ViewModels.Profiles;

public class MemoryRegionProfilesViewModel : ProfileManagementViewModelBase<MemoryMappingProfile>, IDockableViewModel
{
    private readonly IMemoryRegionProfileService _profileService;
    private readonly IUIThreadService _uiThreadService;

    public MemoryRegionProfilesViewModel(
        IMemoryRegionProfileService profileService,
        IUnifiedProfileDialogService unifiedDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService,
        IFileDialogService fileDialogService,
        ILogger<MemoryRegionProfilesViewModel> logger)
        : base(logger, unifiedDialogService, dialogService, uiThreadService, fileDialogService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _uiThreadService = uiThreadService;

        _ = Task.Run(async () =>
        {
            await InitializeAsync();
            RefreshCommand.Execute().Subscribe();
        });
    }

    public string DockId => "MemoryRegionProfiles";
    public string DockTitle => "Memory Regions";
    public bool CanClose => true;
    public bool CanFloat => true;

    protected override IProfileManager<MemoryMappingProfile> GetProfileManager() => _profileService;
    protected override string GetDefaultProfileName() => "Memory Region Default";
    protected override string GetProfileTypeName() => "Memory Region Profile";
    protected override MemoryMappingProfile CreateDefaultProfile() => MemoryMappingProfile.CreateDefaultProfile();

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

    protected override async Task<ProfileDialogResult<MemoryMappingProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        if (SelectedProfile == null)
        {
            return ProfileDialogResult<MemoryMappingProfile>.Failure("No profile selected");
        }

        ILogger<EditMemoryRegionProfileDialogViewModel> dialogLogger = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { }).CreateLogger<EditMemoryRegionProfileDialogViewModel>();
        var dialogViewModel = new EditMemoryRegionProfileDialogViewModel(SelectedProfile, dialogLogger);
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

    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        return await UnifiedDialogService.ShowNameInputDialogAsync(
            $"Duplicate {GetProfileTypeName()}",
            $"Enter a name for the duplicated profile:",
            request.SuggestedName ?? "Copy"
        );
    }
}
