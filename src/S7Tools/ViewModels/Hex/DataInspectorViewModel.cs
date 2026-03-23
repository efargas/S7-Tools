using System;
using System.Buffers.Binary;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using S7Tools.Services.Hex;

namespace S7Tools.ViewModels.Hex;

/// <summary>
/// ViewModel for inspecting selected bytes in various formats.
/// Supports Little Endian (LE) and Big Endian (BE).
/// </summary>
public partial class DataInspectorViewModel : ObservableObject
{
    // Navigation & Editing Actions
    /// <summary>
    /// Gets or sets the RequestGoToOffset.
    /// </summary>
    public Action<long>? RequestGoToOffset { get; set; }
    /// <summary>
    /// Gets or sets the RequestFillSelection.
    /// </summary>
    public Action<byte[]>? RequestFillSelection { get; set; }

    [ObservableProperty]
    private string _targetOffset = "";

    [ObservableProperty]
    private string _fillPattern = "00";

    [RelayCommand]
    private void GoToOffset()
    {
        if (string.IsNullOrWhiteSpace(TargetOffset))
        {
            return;
        }

        // Try parsing hex
        // Support prefixes like 0x
        var scrubbed = TargetOffset.Replace("0x", "").Trim();
        if (long.TryParse(scrubbed, System.Globalization.NumberStyles.HexNumber, null, out long offset))
        {
            RequestGoToOffset?.Invoke(offset);
        }
    }

    [RelayCommand]
    private void FillSelection()
    {
        if (string.IsNullOrWhiteSpace(FillPattern))
        {
            return;
        }

        try
        {
            // Convert hex string "00 01 AB" to byte[]
            // Using Convert.FromHexString which expects "0001AB" (no spaces) or manually parsing.
            // We'll strip common separators.
            var scrubbed = FillPattern.Replace(" ", "").Replace("-", "").Replace(",", "").Replace("0x", "");
            byte[] bytes = Convert.FromHexString(scrubbed);
            RequestFillSelection?.Invoke(bytes);
        }
        catch
        {
            // Ignore parse errors for now, or bind a validation message
        }
    }
    private string _binary8 = "";
    public string Binary8
    {
        get => _binary8;
        set => SetProperty(ref _binary8, value);
    }

    private string _int8 = "";
    public string Int8
    {
        get => _int8;
        set => SetProperty(ref _int8, value);
    }

    private string _uInt8 = "";
    public string UInt8
    {
        get => _uInt8;
        set => SetProperty(ref _uInt8, value);
    }

    private string _int16 = "";
    public string Int16
    {
        get => _int16;
        set => SetProperty(ref _int16, value);
    }

    private string _uInt16 = "";
    public string UInt16
    {
        get => _uInt16;
        set => SetProperty(ref _uInt16, value);
    }

    private string _int32 = "";
    public string Int32
    {
        get => _int32;
        set => SetProperty(ref _int32, value);
    }

    private string _uInt32 = "";
    public string UInt32
    {
        get => _uInt32;
        set => SetProperty(ref _uInt32, value);
    }

    private string _float32 = "";
    public string Float32
    {
        get => _float32;
        set => SetProperty(ref _float32, value);
    }

    private string _double64 = "";
    public string Double64
    {
        get => _double64;
        set => SetProperty(ref _double64, value);
    }

    private bool _isBigEndian = true; // PLC default is usually Big Endian
    public bool IsBigEndian
    {
        get => _isBigEndian;
        set
        {
            if (SetProperty(ref _isBigEndian, value))
            {
                OnIsBigEndianChanged(value);
            }
        }
    }

    private void OnIsBigEndianChanged(bool value)
    {
        // Re-evaluate if we had stored the last bytes, or just wait for next update
        // ideally we store the last bytes to re-render immediately.
        if (_lastBytes != null)
        {
            Update(_lastBytes);
        }
    }

    private byte[]? _lastBytes;

    /// <summary>
    /// Executes the Update operation.
    /// </summary>
    public void Update(byte[]? data)
    {
        _lastBytes = data;

        if (data == null || data.Length == 0)
        {
            Clear();
            return;
        }

        // Binary (first byte)
        Binary8 = Convert.ToString(data[0], 2).PadLeft(8, '0');

        // 8-bit
        Int8 = ((sbyte)data[0]).ToString();
        UInt8 = data[0].ToString();

        // 16-bit
        if (data.Length >= 2)
        {
            short i16 = IsBigEndian ? BinaryPrimitives.ReadInt16BigEndian(data) : BinaryPrimitives.ReadInt16LittleEndian(data);
            ushort u16 = IsBigEndian ? BinaryPrimitives.ReadUInt16BigEndian(data) : BinaryPrimitives.ReadUInt16LittleEndian(data);
            Int16 = i16.ToString();
            UInt16 = u16.ToString();
        }
        else
        { Int16 = "-"; UInt16 = "-"; }

        // 32-bit
        if (data.Length >= 4)
        {
            int i32 = IsBigEndian ? BinaryPrimitives.ReadInt32BigEndian(data) : BinaryPrimitives.ReadInt32LittleEndian(data);
            uint u32 = IsBigEndian ? BinaryPrimitives.ReadUInt32BigEndian(data) : BinaryPrimitives.ReadUInt32LittleEndian(data);

            Int32 = i32.ToString();
            UInt32 = u32.ToString();

            // For float, we need to be careful. BitConverter expects system endianness.
            // If we want BE float, and system is LE, we must reverse.
            byte[] fBytes = data[0..4];
            if (IsBigEndian && BitConverter.IsLittleEndian)
            {
                Array.Reverse(fBytes);
            }

            if (!IsBigEndian && !BitConverter.IsLittleEndian)
            {
                Array.Reverse(fBytes);
            }

            Float32 = BitConverter.ToSingle(fBytes).ToString("G");
        }
        else
        { Int32 = "-"; UInt32 = "-"; Float32 = "-"; }

        // 64-bit
        if (data.Length >= 8)
        {
            byte[] dBytes = data[0..8];
            if (IsBigEndian && BitConverter.IsLittleEndian)
            {
                Array.Reverse(dBytes);
            }

            if (!IsBigEndian && !BitConverter.IsLittleEndian)
            {
                Array.Reverse(dBytes);
            }

            Double64 = BitConverter.ToDouble(dBytes).ToString("G");
        }
        else
        { Double64 = "-"; }
    }

    private void Clear()
    {
        Binary8 = "-";
        Int8 = "-";
        UInt8 = "-";
        Int16 = "-";
        UInt16 = "-";
        Int32 = "-";
        UInt32 = "-";
        Float32 = "-";
        Double64 = "-";
    }

    // Helpers not strictly needed with the array logic above but good for clarity if reused
    private static byte[] ReverseIfBig(byte[] b) { if (!BitConverter.IsLittleEndian) { Array.Reverse(b); } return b; }

    /// <summary>
    /// Gets or sets the Search.
    /// </summary>
    public SearchViewModel Search { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataInspectorViewModel"/> class.
    /// </summary>
    public DataInspectorViewModel()
    {
        // Initialize SearchViewModel
        Search = new SearchViewModel(new BinarySearchService());
        Search.RequestNavigation += OnSearchRequestNavigation;
    }

    private void OnSearchRequestNavigation(long offset)
    {
        RequestGoToOffset?.Invoke(offset);
    }

    /// <summary>
    /// Executes the SetDocument operation.
    /// </summary>
    public void SetDocument(AvaloniaHex.Document.IBinaryDocument? document)
    {
        Search.SetDocument(document);
    }
}
