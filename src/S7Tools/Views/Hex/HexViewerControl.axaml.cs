using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Data;
using AvaloniaHex.Document;
using Avalonia.Markup.Xaml;
using S7Tools.ViewModels.Hex;
using AvaloniaHex.Rendering;
using Avalonia.Threading;

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

        // Use code-behind layout calculation to enable horizontal scrolling
        MainHexEditor.LayoutUpdated += MainHexEditor_LayoutUpdated;
        MainHexEditor.HexView.BytesPerLine = 16;

        // Initial width calculation trigger
        Dispatcher.UIThread.Post(() => MainHexEditor.InvalidateMeasure(), DispatcherPriority.Loaded);
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

    // Toggle Column Visibility
    private void ToggleColumn<TColumn>() where TColumn : Column
    {
        var column = MainHexEditor.Columns.Get<TColumn>();
        column.IsVisible = !column.IsVisible;
    }

    private void ToggleOffsetColumnOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<OffsetColumn>();
    private void ToggleHexColumnOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<HexColumn>();
    private void ToggleBinaryColumnOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<BinaryColumn>();
    private void ToggleAsciiColumnOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<AsciiColumn>();

    // Toggle Header Visibility
    private void ToggleHeaderVisibleOnClick(object? sender, RoutedEventArgs e)
    {
        MainHexEditor.IsHeaderVisible = !MainHexEditor.IsHeaderVisible;
    }

    private void ToggleColumnHeader<TColumn>() where TColumn : Column
    {
        var column = MainHexEditor.Columns.Get<TColumn>();
        column.IsHeaderVisible = !column.IsHeaderVisible;
    }

    private void ToggleOffsetHeaderVisibleOnClick(object? sender, RoutedEventArgs e) => ToggleColumnHeader<OffsetColumn>();
    private void ToggleHexHeaderVisibleOnClick(object? sender, RoutedEventArgs e) => ToggleColumnHeader<HexColumn>();
    private void ToggleBinaryHeaderVisibleOnClick(object? sender, RoutedEventArgs e) => ToggleColumnHeader<BinaryColumn>();
    private void ToggleAsciiHeaderVisibleOnClick(object? sender, RoutedEventArgs e) => ToggleColumnHeader<AsciiColumn>();

    private void ColumnPaddingOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: { } param } &&
            double.TryParse(param.ToString(), out double padding))
        {
            MainHexEditor.ColumnPadding = padding;
        }
    }

    private void BytesPerLineOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: { } param } &&
            int.TryParse(param.ToString(), out int count))
        {
            MainHexEditor.HexView.BytesPerLine = count;
        }
    }

    private void FontSizeOnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { CommandParameter: { } param } &&
            double.TryParse(param.ToString(), out double size))
        {
            MainHexEditor.HexView.FontSize = size;
        }
    }

    private bool _isResizing;
    private void MainHexEditor_LayoutUpdated(object? sender, EventArgs e)
    {
        if (_isResizing) return;

        try
        {
            _isResizing = true;

            // Calculate total required width based on columns
            double totalWidth = 0;
            double padding = MainHexEditor.ColumnPadding;
            int visibleColumns = 0;

            foreach (var column in MainHexEditor.Columns)
            {
                if (column.IsVisible)
                {
                    totalWidth += column.Width;
                    visibleColumns++;
                }
            }

            if (visibleColumns > 0)
            {
                totalWidth += (visibleColumns - 1) * padding + 100; // Increase buffer to 100
            }

            // Ensure we at least cover the basic 16 bytes + address + ascii
            if (totalWidth < 800) totalWidth = 800;

            // Update MinWidth instead of Width to allow stretching.
            // This ensures the control is at least as wide as content (scrolling happens if container < MinWidth)
            // but stretches if container > MinWidth.
            if (double.IsNaN(MainHexEditor.MinWidth) || Math.Abs(MainHexEditor.MinWidth - totalWidth) > 5)
            {
                MainHexEditor.MinWidth = totalWidth;
                // Clear Width if it was set (just in case) to ensure Stretch works
                if (!double.IsNaN(MainHexEditor.Width))
                {
                    MainHexEditor.Width = double.NaN;
                }
            }
        }
        finally
        {
            _isResizing = false;
        }
    }

}
