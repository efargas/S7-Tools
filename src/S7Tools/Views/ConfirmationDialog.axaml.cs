using Avalonia.Controls;
using S7Tools.ViewModels;
using System;

namespace S7Tools.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog()
    {
        InitializeComponent();
        
        // Subscribe to command results to close the dialog
        DataContextChanged += OnDataContextChanged;
    }
    
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ConfirmationDialogViewModel viewModel)
        {
            // Subscribe to command executions
            viewModel.OkCommand.Subscribe(result => Close(result));
            viewModel.CancelCommand.Subscribe(result => Close(result));
        }
    }
}