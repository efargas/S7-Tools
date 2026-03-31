using S7Tools.ViewModels.Dialogs;

namespace S7Tools.Views.Dialogs;

/// <summary>
/// Dialog for duplicating an existing memory region profile.
/// Provides UI for setting a new name, description, and duplication options.
/// </summary>
public partial class DuplicateMemoryRegionProfileDialog : Window
{
    public DuplicateMemoryRegionProfileDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the dialog with the specified ViewModel.
    /// </summary>
    /// <param name="viewModel">The ViewModel containing dialog logic and data.</param>
    public void Initialize(DuplicateMemoryRegionProfileDialogViewModel viewModel)
    {
        if (viewModel == null)
        {
            throw new ArgumentNullException(nameof(viewModel));
        }

        DataContext = viewModel;

        // Subscribe to ViewModel events for dialog lifecycle management
        viewModel.CloseRequested += OnCloseRequested;

        // Handle window closing to clean up subscriptions
        Closing += OnClosing;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCloseRequested(object? sender, bool success)
    {
        Close(success);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Clean up ViewModel event subscriptions
        if (DataContext is DuplicateMemoryRegionProfileDialogViewModel viewModel)
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }
    }
}
