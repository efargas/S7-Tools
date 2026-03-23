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

public class SerialPortProfilesViewModel : ProfileManagementViewModelBase<SerialPortProfile>, IDockableViewModel
{
    private readonly ISerialPortProfileService _profileService;

    public SerialPortProfilesViewModel(
        ISerialPortProfileService profileService,
        IUnifiedProfileDialogService unifiedDialogService,
        IDialogService dialogService,
        IUIThreadService uiThreadService,
        IFileDialogService fileDialogService,
        ILogger<SerialPortProfilesViewModel> logger)
        : base(logger, unifiedDialogService, dialogService, uiThreadService, fileDialogService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));

        _ = Task.Run(async () =>
        {
            await InitializeAsync();
        });
    }

    public string DockId => "SerialPortProfiles";
    public string DockTitle => "Serial Ports";
    public bool CanClose => true;
    public bool CanFloat => true;

    protected override IProfileManager<SerialPortProfile> GetProfileManager() => _profileService;
    protected override string GetDefaultProfileName() => "SerialDefault";
    protected override string GetProfileTypeName() => "Serial Port";
    protected override SerialPortProfile CreateDefaultProfile() => SerialPortProfile.CreateDefaultProfile();

    protected override async Task<ProfileDialogResult<SerialPortProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        return await UnifiedDialogService.ShowSerialCreateDialogAsync(request).ConfigureAwait(false);
    }

    protected override async Task<ProfileDialogResult<SerialPortProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        return await UnifiedDialogService.ShowSerialEditDialogAsync(request).ConfigureAwait(false);
    }

    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        return await UnifiedDialogService.ShowSerialDuplicateDialogAsync(request).ConfigureAwait(false);
    }
}
