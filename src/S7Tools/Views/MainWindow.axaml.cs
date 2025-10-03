using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;
using S7Tools.ViewModels;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;

namespace S7Tools.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is null) return;
            ViewModel.CloseApplicationInteraction.RegisterHandler(interaction =>
            {
                Close();
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposables);
        });
    }

    private void NavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (ViewModel is null) return;

        if (e.SelectedItem is NavigationViewItem selectedItem)
        {
            // Assuming ViewModel.SelectedNavigationItem is already updated via binding
            // We need to compare the newly selected item with the previously selected item
            // to determine if we should expand or collapse the pane.

            // This logic needs to be handled in the ViewModel to properly update IsLeftPanelOpen
            // based on the selection. For now, we'll just ensure the pane is open when a new item is selected.
            ViewModel.IsLeftPanelOpen = true; // Always open when a new item is selected
        }
    }
}