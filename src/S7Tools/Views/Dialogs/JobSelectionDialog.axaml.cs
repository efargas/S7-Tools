using S7Tools.ViewModels.Dialogs;

namespace S7Tools.Views.Dialogs;

/// <summary>
/// Dialog for selecting a job profile to create a new task.
/// </summary>
public partial class JobSelectionDialog : Window
{
    public JobSelectionDialog()
    {
        InitializeComponent();

        // Wire up commands to close the dialog with results
        this.Opened += (s, e) =>
        {
            if (DataContext is JobSelectionDialogViewModel viewModel)
            {
                // Subscribe to Confirm command - close with selected job
                viewModel.ConfirmCommand.Subscribe(_ =>
                {
                    Close(viewModel.SelectedJob);
                });

                // Subscribe to Cancel command - close with null
                viewModel.CancelCommand.Subscribe(_ =>
                {
                    Close(null);
                });
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
