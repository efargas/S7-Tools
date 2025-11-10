using Avalonia.Controls;
using System;

namespace S7Tools.Views.Jobs;

/// <summary>
/// View for the memory region profile selection step in the job wizard.
/// </summary>
public partial class JobWizardMemoryRegionStepView : UserControl
{
    public JobWizardMemoryRegionStepView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Console.WriteLine($"[JobWizardMemoryRegionStepView] DataContext changed to: {DataContext?.GetType().Name ?? "null"}");
        if (DataContext != null)
        {
            Console.WriteLine($"[JobWizardMemoryRegionStepView] DataContext is: {DataContext}");
        }
    }
}
