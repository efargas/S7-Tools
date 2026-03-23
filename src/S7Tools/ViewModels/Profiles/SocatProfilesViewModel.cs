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

namespace S7Tools.ViewModels.Profiles;

public class SocatProfilesViewModel : ProfileManagementViewModelBase<SocatProfile>, IDockableViewModel
{
    private readonly ISocatProfileService _profileService;

    public SocatProfilesViewModel(
        ISocatProfileService profileService,
        IUnifiedProfileDialogService unifiedDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService,
        IFileDialogService fileDialogService,
        ILogger<SocatProfilesViewModel> logger)
        : base(logger, unifiedDialogService, dialogService, uiThreadService, fileDialogService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

        _ = Task.Run(async () =>
        {
            await InitializeAsync();
        });
    }

    public string DockId => "SocatProfiles";
    public string DockTitle => "Servers";
    public bool CanClose => true;
    public bool CanFloat => true;

    protected override IProfileManager<SocatProfile> GetProfileManager() => _profileService;
    protected override string GetDefaultProfileName() => "ServerDefault";
    protected override string GetProfileTypeName() => "Server Configuration";
    protected override SocatProfile CreateDefaultProfile() => SocatProfile.CreateDefaultProfile();

    protected override async Task<ProfileDialogResult<SocatProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        return await UnifiedDialogService.ShowSocatCreateDialogAsync(request).ConfigureAwait(false);
    }

    protected override async Task<ProfileDialogResult<SocatProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        return await UnifiedDialogService.ShowSocatEditDialogAsync(request).ConfigureAwait(false);
    }

    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        return await UnifiedDialogService.ShowSocatDuplicateDialogAsync(request).ConfigureAwait(false);
    }
}
