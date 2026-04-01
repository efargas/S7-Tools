using System.Reactive;
using ReactiveUI;
using S7Tools.Core.Models.Jobs;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels.Dialogs.Models;

namespace S7Tools.Services;

/// <summary>
/// A stub implementation of IDialogService for design-time use.
/// </summary>
public class DesignTimeDialogService : IDialogService
{
    /// <summary>
    /// Gets or sets the ShowConfirmation.
    /// </summary>
    public Interaction<ConfirmationRequest, bool> ShowConfirmation { get; } = new();
    /// <summary>
    /// Gets or sets the ShowError.
    /// </summary>
    public Interaction<ConfirmationRequest, Unit> ShowError { get; } = new();
    /// <summary>
    /// Gets or sets the ShowInput.
    /// </summary>
    public Interaction<InputRequest, InputResult> ShowInput { get; } = new();
    /// <summary>
    /// Gets or sets the ShowJobSelection.
    /// </summary>
    public Interaction<JobSelectionRequest, JobProfile?> ShowJobSelection { get; } = new();

    /// <summary>
    /// Executes the ShowConfirmationAsync operation.
    /// </summary>
    public Task<bool> ShowConfirmationAsync(string title, string message) => Task.FromResult(false);
    /// <summary>
    /// Executes the ShowErrorAsync operation.
    /// </summary>
    public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;
    /// <summary>
    /// Executes the ShowInputAsync operation.
    /// </summary>
    public Task<InputResult> ShowInputAsync(string title, string message, string? defaultValue = null, string? placeholder = null)
        => Task.FromResult(InputResult.Cancelled());
    /// <summary>
    /// Executes the ShowJobSelectionAsync operation.
    /// </summary>
    public Task<JobProfile?> ShowJobSelectionAsync() => Task.FromResult<JobProfile?>(null);
}
