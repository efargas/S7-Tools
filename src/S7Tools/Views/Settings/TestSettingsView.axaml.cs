using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ReactiveUI;
using S7Tools.ViewModels.Settings;

namespace S7Tools.Views.Settings;

public partial class TestSettingsView : ReactiveUserControl<TestSettingsViewModel>
{
    public TestSettingsView()
    {
        InitializeComponent();

        // Force Selection Synchronization:
        // Avalonia's SelectionStart/End bindings may not update the VM instantly while the TextBox has focus.
        // We force the update on SelectionChanged to ensure Cut/Copy logic works correctly.
        TextBox? textBox = this.FindControl<TextBox>("ClipboardTextBox");
        if (textBox != null)
        {
            textBox.PropertyChanged += (s, e) =>
            {
                if (e.Property.Name == "SelectionStart" || 
                    e.Property.Name == "SelectionEnd" || 
                    e.Property.Name == "CaretIndex")
                {
                    if (ViewModel != null)
                    {
                        ViewModel.SelectionStart = textBox.SelectionStart;
                        ViewModel.SelectionEnd = textBox.SelectionEnd;
                        ViewModel.CaretIndex = textBox.CaretIndex;
                    }
                }
            };
        }
    }
}
