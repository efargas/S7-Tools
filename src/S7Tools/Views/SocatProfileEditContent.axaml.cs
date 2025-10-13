using Avalonia.Controls;
using S7Tools.ViewModels;

namespace S7Tools.Views;

/// <summary>
/// UserControl for editing socat profile properties with comprehensive form layout and data binding.
/// </summary>
public partial class SocatProfileEditContent : UserControl
{
    /// <summary>
    /// Initializes a new instance of the SocatProfileEditContent class.
    /// </summary>
    public SocatProfileEditContent()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the ViewModel for the socat profile being edited.
    /// </summary>
    public SocatProfileViewModel? ViewModel
    {
        get => DataContext as SocatProfileViewModel;
        set => DataContext = value;
    }
}
