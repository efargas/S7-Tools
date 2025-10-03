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

    private void MainNavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && e.SelectedItem is NavigationItemViewModel selectedItem)
        {
            viewModel.NavigateTo(selectedItem.ContentViewModelType);
        }
    }
}