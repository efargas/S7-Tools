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
    private NavigationItemViewModel? _previousSelectedItem;
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

    private void MainNavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && e.SelectedItem is NavigationItemViewModel selectedItem)
        {
            if (_previousSelectedItem == selectedItem)
            {
                var navigationView = this.FindControl<NavigationView>("MainNavigationView");
                if (navigationView != null)
                {
                    navigationView.IsPaneOpen = !navigationView.IsPaneOpen;
                }
            }
            else
            {
                viewModel.NavigateTo(selectedItem.ContentViewModelType);
                var navigationView = this.FindControl<NavigationView>("MainNavigationView");
                if (navigationView != null)
                {
                    navigationView.IsPaneOpen = true;
                }
            }

            _previousSelectedItem = selectedItem;
        }
    }
}