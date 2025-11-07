using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using S7Tools.ViewModels;
using S7Tools.ViewModels.Layout;

namespace S7Tools.Views.Layout;

/// <summary>
/// Main window with VSCode-style layout.
/// </summary>
public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        SetupInteractionHandlers();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class with service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public MainWindow(IServiceProvider serviceProvider) : this()
    {
        // Service provider is passed but not used here since interactions are registered globally
    }

    /// <summary>
    /// Sets up interaction handlers for window-specific interactions.
    /// </summary>
    private void SetupInteractionHandlers()
    {
        this.WhenActivated(disposables =>
        {
            if (ViewModel is null)
            {
                return;
            }

            // Handle application close interaction
            ViewModel.CloseApplicationInteraction.RegisterHandler(interaction =>
            {
                Close();
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposables);
        });
    }

    private bool _isClosingConfirmed;

    /// <summary>
    /// Handles the window closing event to ensure confirmation dialog is shown for all exit methods.
    /// </summary>
    /// <param name="e">The window closing event args.</param>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // If we already confirmed the close or no ViewModel exists, proceed normally
        if (_isClosingConfirmed || ViewModel is null)
        {
            base.OnClosing(e);
            return;
        }

        // Cancel the close operation to show confirmation dialog
        e.Cancel = true;

        // Request confirmation and shutdown from ViewModel
        _ = RequestConfirmationAndShutdownAsync();
    }

    /// <summary>
    /// Requests confirmation and shutdown from the ViewModel.
    /// </summary>
    private async Task RequestConfirmationAndShutdownAsync()
    {
        try
        {
            if (ViewModel is null)
            {
                return;
            }

            // Show confirmation dialog via ViewModel
            bool result = await ViewModel.ShowExitConfirmationAsync();

            if (result)
            {
                // User confirmed, proceed with shutdown and close
                await ViewModel.PerformShutdownAsync();

                // Mark as confirmed and close on UI thread
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _isClosingConfirmed = true;
                    Close();
                });
            }
            // If user cancels, do nothing - window stays open
        }
        catch (Exception ex)
        {
            // Log any errors and allow normal close
            System.Diagnostics.Debug.WriteLine($"Error during confirmation and shutdown: {ex.Message}");

            // Force close on the UI thread as last resort
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                _isClosingConfirmed = true;
                Close();
            });
        }
    }
}
