using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using S7Tools.Core.Interfaces.Services;
using S7Tools.Core.Interfaces.ViewModels;
using S7Tools.Core.Models;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Base;
using S7Tools.ViewModels.Dialogs;

namespace S7Tools.ViewModels.Profiles;

/// <summary>
/// Represents the SocatProfilesViewModel.
/// </summary>
public class SocatProfilesViewModel : ProfileManagementViewModelBase<SocatProfile>, IDockableViewModel
{
    private readonly ISocatProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocatProfilesViewModel"/> class.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "SocatProfiles";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Servers";
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
    protected override IProfileManager<SocatProfile> GetProfileManager() => _profileService;
    /// <summary>
    /// Executes the GetDefaultProfileName operation.
    /// </summary>
    protected override string GetDefaultProfileName() => "ServerDefault";
    /// <summary>
    /// Executes the GetProfileTypeName operation.
    /// </summary>
    protected override string GetProfileTypeName() => "Server Configuration";
    /// <summary>
    /// Executes the CreateDefaultProfile operation.
    /// </summary>
    protected override SocatProfile CreateDefaultProfile() => SocatProfile.CreateDefaultProfile();

    /// <summary>
    /// Executes the ShowCreateDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<SocatProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        return await UnifiedDialogService.ShowSocatCreateDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the ShowEditDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<SocatProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        return await UnifiedDialogService.ShowSocatEditDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the ShowDuplicateDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        return await UnifiedDialogService.ShowSocatDuplicateDialogAsync(request).ConfigureAwait(false);
    }
}
