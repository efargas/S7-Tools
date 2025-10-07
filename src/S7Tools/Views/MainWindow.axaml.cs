using System.Reactive;
using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using ReactiveUI;
using S7Tools.ViewModels;
using System;

namespace S7Tools.Views;

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
            if (ViewModel is null) return;
            
            // Handle application close interaction
            ViewModel.CloseApplicationInteraction.RegisterHandler(interaction =>
            {
                Close();
                interaction.SetOutput(Unit.Default);
            }).DisposeWith(disposables);
        });
    }
}