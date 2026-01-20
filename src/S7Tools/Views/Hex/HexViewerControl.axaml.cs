using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Data;
using AvaloniaHex.Document;
using Avalonia.Markup.Xaml;
using S7Tools.ViewModels.Hex;

namespace S7Tools.Views.Hex;

public partial class HexViewerControl : UserControl
{
    public HexViewerControl()
    {
        InitializeComponent();

        // Workaround for missing BytesPerLine dependency property on HexEditor in AvaloniaHex 0.1.11
        MainHexEditor.HexView.Bind(AvaloniaHex.Rendering.HexView.BytesPerLineProperty, new Binding("BytesPerLine"));

        // Set up event handlers
        MainHexEditor.Selection.RangeChanged += OnSelectionRangeChanged;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is HexViewerViewModel vm)
        {
            vm.NavigateTo -= OnNavigateTo; // Unsubscribe to avoid duplicates if context switches (rare)
            vm.NavigateTo += OnNavigateTo;
        }
    }

    private void OnNavigateTo(long offset)
    {
        MainHexEditor.Caret.Location = new AvaloniaHex.Document.BitLocation((ulong)offset);
        MainHexEditor.Focus();
    }

    private void OnSelectionRangeChanged(object? sender, EventArgs e)
    {
        if (DataContext is HexViewerViewModel vm)
        {
            vm.SelectionStart = (long)MainHexEditor.Selection.Range.Start.ByteIndex;
            vm.SelectionLength = (long)MainHexEditor.Selection.Range.ByteLength;
        }
    }

    private async void CopyOnClick(object? sender, RoutedEventArgs e)
    {
        await MainHexEditor.Copy();
    }

    private void FontSizeOnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HexViewerViewModel vm &&
            sender is MenuItem { CommandParameter: { } param } &&
            double.TryParse(param.ToString(), out double size))
        {
            vm.FontSize = size;
        }
    }

    private void BytesPerLineOnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HexViewerViewModel vm &&
            sender is MenuItem { CommandParameter: { } param } &&
            int.TryParse(param.ToString(), out int count))
        {
            vm.BytesPerLine = count;
        }
    }

    private void ColumnPaddingOnClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is HexViewerViewModel vm &&
            sender is MenuItem { CommandParameter: { } param } &&
            double.TryParse(param.ToString(), out double padding))
        {
            vm.ColumnPadding = padding;
        }
    }

    // Toggle handlers are managed via Binding IsChecked in XAML, 
    // but we need these methods to satisfy the Click event handlers defined in XAML.
    private void ToggleOffsetColumnOnClick(object? sender, RoutedEventArgs e) { }
    private void ToggleHexColumnOnClick(object? sender, RoutedEventArgs e) { }
    private void ToggleBinaryColumnOnClick(object? sender, RoutedEventArgs e) { }
    private void ToggleAsciiColumnOnClick(object? sender, RoutedEventArgs e) { }

    private void ToggleHeaderVisibleOnClick(object? sender, RoutedEventArgs e) { }
    private void ToggleOffsetHeaderVisibleOnClick(object? sender, RoutedEventArgs e) { }
    private void ToggleHexHeaderVisibleOnClick(object? sender, RoutedEventArgs e) { }
    private void ToggleBinaryHeaderVisibleOnClick(object? sender, RoutedEventArgs e) { }
    private void ToggleAsciiHeaderVisibleOnClick(object? sender, RoutedEventArgs e) { }
}
