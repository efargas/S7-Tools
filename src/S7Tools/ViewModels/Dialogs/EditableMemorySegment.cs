using System.ComponentModel;
using ReactiveUI;
using S7Tools.Core.Models;

namespace S7Tools.ViewModels.Dialogs;

/// <summary>
/// Wrapper for MemorySegment that provides editable Start/End address and Size with automatic calculations.
/// </summary>
/// <remarks>
/// This class ensures proper validation and automatic calculation of:
/// - End Address when Start Address or Size changes
/// - Size when Start Address or End Address changes
/// All three properties (Start, End, Size) are independently editable with bidirectional calculations.
/// </remarks>
public class EditableMemorySegment : ReactiveObject
{
    private readonly MemorySegment _segment;
    private string _startAddressText;
    private string _endAddressText;
    private string _sizeText;
    private bool _isUpdating;

    /// <summary>
    /// Initializes a new instance of the EditableMemorySegment class.
    /// </summary>
    /// <param name="segment">The underlying memory segment.</param>
    public EditableMemorySegment(MemorySegment segment)
    {
        _segment = segment ?? throw new ArgumentNullException(nameof(segment));
        _startAddressText = segment.StartAddress;
        _sizeText = MemorySegment.FormatAddress(segment.Size);

        // Calculate initial end address
        // IMPORTANT: End Address = Start Address + Size (exclusive end, first byte AFTER the segment)
        // Example: Start=0x08000000, Size=1 → End=0x08000001 (segment contains only byte at 0x08000000)
        // Example: Start=0x08000000, Size=0 → End=0x08000000 (empty segment)
        try
        {
            long startAddr = MemorySegment.ParseAddress(_startAddressText);
            long endAddr = startAddr + segment.Size;
            _endAddressText = MemorySegment.FormatAddress(endAddr);
        }
        catch
        {
            _endAddressText = "0x00000000";
        }

        // Subscribe to segment property changes
        _segment.PropertyChanged += OnSegmentPropertyChanged;
    }

    #region Properties

    /// <summary>
    /// Gets the underlying memory segment.
    /// </summary>
    public MemorySegment Segment => _segment;

    /// <summary>
    /// Gets or sets the segment name.
    /// </summary>
    public string Name
    {
        get => _segment.Name;
        set
        {
            if (_segment.Name != value)
            {
                _segment.Name = value;
                this.RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the start address as text (editable).
    /// </summary>
    public string StartAddress
    {
        get => _startAddressText;
        set
        {
            if (_isUpdating)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _startAddressText, value);
            _isUpdating = true;
            try
            {
                _segment.StartAddress = value;
                RecalculateEndAddress();
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets the end address as text (editable).
    /// </summary>
    public string EndAddress
    {
        get => _endAddressText;
        set
        {
            if (_isUpdating)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _endAddressText, value);
            _isUpdating = true;
            try
            {
                RecalculateSizeFromEndAddress();
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets the size in hexadecimal format (editable).
    /// </summary>
    public string Size
    {
        get => _sizeText;
        set
        {
            if (_isUpdating)
            {
                return;
            }

            this.RaiseAndSetIfChanged(ref _sizeText, value);
            _isUpdating = true;
            try
            {
                long sizeBytes = MemorySegment.ParseAddress(value);
                if (sizeBytes >= 0) // Size can be 0 or positive
                {
                    _segment.Size = sizeBytes;
                    RecalculateEndAddress();
                }
            }
            catch
            {
                // Invalid hex format, don't update
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

    /// <summary>
    /// Gets the formatted size display (read-only).
    /// </summary>
    public string SizeFormatted => _segment.SizeFormatted;

    /// <summary>
    /// Gets or sets the memory segment type.
    /// </summary>
    public MemorySegmentType Type
    {
        get => _segment.Type;
        set
        {
            if (_segment.Type != value)
            {
                _segment.Type = value;
                this.RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether this segment is selected.
    /// </summary>
    public bool IsSelected
    {
        get => _segment.IsSelected;
        set
        {
            if (_segment.IsSelected != value)
            {
                _segment.IsSelected = value;
                this.RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the segment description.
    /// </summary>
    public string Description
    {
        get => _segment.Description;
        set
        {
            if (_segment.Description != value)
            {
                _segment.Description = value;
                this.RaisePropertyChanged();
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Recalculates the end address based on start address and size.
    /// </summary>
    /// <remarks>
    /// Formula: End Address = Start Address + Size (exclusive end)
    /// Example: Start=0x08000000, Size=0x100 (256 bytes) → End=0x08000100
    /// The end address points to the first byte AFTER the segment (exclusive).
    /// </remarks>
    private void RecalculateEndAddress()
    {
        try
        {
            long startAddr = MemorySegment.ParseAddress(_startAddressText);
            long sizeBytes = MemorySegment.ParseAddress(_sizeText);

            // Calculate end address: End = Start + Size (exclusive end)
            long endAddr = startAddr + sizeBytes;

            string newEndAddress = MemorySegment.FormatAddress(endAddr);
            if (_endAddressText != newEndAddress)
            {
                _endAddressText = newEndAddress;
                this.RaisePropertyChanged(nameof(EndAddress));
            }
        }
        catch
        {
            // Invalid address format, don't update
        }
    }

    /// <summary>
    /// Recalculates the size based on start and end addresses.
    /// </summary>
    /// <remarks>
    /// Formula: Size = End Address - Start Address
    /// Example: Start=0x08000000, End=0x08000100 → Size=0x100 (256 bytes)
    /// Special case: Start=End → Size=0 (empty segment)
    /// </remarks>
    private void RecalculateSizeFromEndAddress()
    {
        try
        {
            long startAddr = MemorySegment.ParseAddress(_startAddressText);
            long endAddr = MemorySegment.ParseAddress(_endAddressText);

            if (endAddr < startAddr)
            {
                // Invalid: end before start, don't update
                return;
            }

            // Calculate size: Size = End - Start
            // If start == end, size is 0 (empty segment)
            long newSize = endAddr - startAddr;

            string newSizeText = MemorySegment.FormatAddress(newSize);
            if (_sizeText != newSizeText)
            {
                _sizeText = newSizeText;
                _segment.Size = newSize;
                this.RaisePropertyChanged(nameof(Size));
                this.RaisePropertyChanged(nameof(SizeFormatted));
            }
        }
        catch
        {
            // Invalid address format, don't update
        }
    }

    /// <summary>
    /// Handles property changes from the underlying segment.
    /// </summary>
    private void OnSegmentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        // Forward relevant property changes
        switch (e.PropertyName)
        {
            case nameof(MemorySegment.IsSelected):
                this.RaisePropertyChanged(nameof(IsSelected));
                break;
        }
    }

    #endregion
}
