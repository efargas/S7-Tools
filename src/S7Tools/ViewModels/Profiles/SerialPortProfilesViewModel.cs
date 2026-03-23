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
/// Represents the SerialPortProfilesViewModel.
/// </summary>
public class SerialPortProfilesViewModel : ProfileManagementViewModelBase<SerialPortProfile>, IDockableViewModel
{
    private readonly ISerialPortProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerialPortProfilesViewModel"/> class.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the DockId.
    /// </summary>
    public string DockId => "SerialPortProfiles";
    /// <summary>
    /// Gets or sets the DockTitle.
    /// </summary>
    public string DockTitle => "Serial Ports";
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
    protected override IProfileManager<SerialPortProfile> GetProfileManager() => _profileService;
    /// <summary>
    /// Executes the GetDefaultProfileName operation.
    /// </summary>
    protected override string GetDefaultProfileName() => "SerialDefault";
    /// <summary>
    /// Executes the GetProfileTypeName operation.
    /// </summary>
    protected override string GetProfileTypeName() => "Serial Port";
    /// <summary>
    /// Executes the CreateDefaultProfile operation.
    /// </summary>
    protected override SerialPortProfile CreateDefaultProfile() => SerialPortProfile.CreateDefaultProfile();

    /// <summary>
    /// Executes the ShowCreateDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<SerialPortProfile>> ShowCreateDialogAsync(ProfileCreateRequest request)
    {
        return await UnifiedDialogService.ShowSerialCreateDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the ShowEditDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<SerialPortProfile>> ShowEditDialogAsync(ProfileEditRequest request)
    {
        return await UnifiedDialogService.ShowSerialEditDialogAsync(request).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the ShowDuplicateDialogAsync operation.
    /// </summary>
    protected override async Task<ProfileDialogResult<string>> ShowDuplicateDialogAsync(ProfileDuplicateRequest request)
    {
        return await UnifiedDialogService.ShowSerialDuplicateDialogAsync(request).ConfigureAwait(false);
    }
}
