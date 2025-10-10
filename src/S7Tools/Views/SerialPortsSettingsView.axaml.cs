using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using S7Tools.Core.Models;
using S7Tools.ViewModels;

namespace S7Tools.Views;

/// <summary>
/// Code-behind for the SerialPortsSettingsView.
/// </summary>
public partial class SerialPortsSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the SerialPortsSettingsView class.
    /// </summary>
    public SerialPortsSettingsView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles the CellEditEnding event to intercept IsDefault property changes.
    /// </summary>
    /// <param name="sender">The DataGrid that triggered the event.</param>
    /// <param name="e">The event arguments containing edit information.</param>
    private async void DataGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        // Check if this is an IsDefault checkbox change
        if (e.Column.Header?.ToString() == "Default" && e.EditAction == DataGridEditAction.Commit)
        {
            // Get the profile from the row
            if (e.Row.DataContext is SerialPortProfile profile && DataContext is SerialPortsSettingsViewModel viewModel)
            {
                // Only proceed if the checkbox is being checked (set to true)
                if (e.EditingElement is CheckBox checkBox && checkBox.IsChecked == true)
                {
                    // Ensure the profile is selected first
                    viewModel.SelectedProfile = profile;

                    // Execute the command and wait for completion
                    try
                    {
                        await viewModel.SetDefaultProfileCommand.Execute();
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't throw to avoid disrupting the UI
                        System.Diagnostics.Debug.WriteLine($"Error setting default profile: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts the binding path from a binding object.
    /// </summary>
    /// <param name="binding">The binding object.</param>
    /// <returns>The binding path as a string.</returns>
    private static string GetBindingPath(object binding)
    {
        return binding switch
        {
            Avalonia.Data.Binding avaloniaBind => avaloniaBind.Path ?? string.Empty,
            _ => string.Empty
        };
    }
}
