using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;
using S7Tools.Views;

namespace S7Tools.Services;

/// <summary>
/// A service for displaying dialogs.
/// </summary>
public class DialogService : IDialogService
{
    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var dialog = new ConfirmationDialog();
        dialog.DataContext = new ConfirmationDialogViewModel(dialog, title, message);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            return await dialog.ShowDialog<bool>(desktop.MainWindow);
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string title, string message)
    {
        // For now, use the confirmation dialog as an error dialog
        // In a real implementation, you would create a dedicated error dialog
        var dialog = new ConfirmationDialog();
        dialog.DataContext = new ConfirmationDialogViewModel(dialog, title, message);

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is not null)
        {
            await dialog.ShowDialog<bool>(desktop.MainWindow);
        }
    }
}