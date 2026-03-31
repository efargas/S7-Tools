using Avalonia.ReactiveUI;
using S7Tools.ViewModels.Settings;

namespace S7Tools.Views.Settings;

public partial class TestSettingsView : ReactiveUserControl<TestSettingsViewModel>
{
    public TestSettingsView()
    {
        InitializeComponent();
    }
}
