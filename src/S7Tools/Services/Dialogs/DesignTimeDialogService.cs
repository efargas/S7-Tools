using System.Reactive;
using System.Threading.Tasks;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Models;
using S7Tools.ViewModels.Dialogs.Models;
using S7Tools.Services.Interfaces;

namespace S7Tools.Services;

/// <summary>
/// A stub implementation of IDialogService for design-time use.
/// </summary>
public class DesignTimeDialogService : IDialogService
{
    public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; } = new();
    public Interaction<ConfirmationRequest, Unit> ShowError { get; } = new();
    public Interaction<InputRequest, InputResult> ShowInput { get; } = new();
    public Interaction<JobSelectionRequest, JobProfile?> ShowJobSelection { get; } = new();

    public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    public Task<InputResult> ShowInputAsync(string title, string message, string? defaultValue = null, string? placeholder = null)
        => Task.FromResult(InputResult.Cancelled());
    public Task<JobProfile?> ShowJobSelectionAsync() => Task.FromResult<JobProfile?>(null);
}
