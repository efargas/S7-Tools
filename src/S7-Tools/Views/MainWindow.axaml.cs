using Avalonia.Controls;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive;

namespace S7_Tools.Views;

public partial class MainWindow : Window, IActivatableView
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (DataContext is ViewModels.MainWindowViewModel viewModel)
            {
                viewModel.CloseApplicationInteraction.RegisterHandler(interaction =>
                {
                    Close();
                    interaction.SetOutput(Unit.Default);
                }).DisposeWith(disposables);
            }
        });
    }
}