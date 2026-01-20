using System;
using System.IO;
using System.Reactive;
using System.Reactive.Disposables;
using AvaloniaHex.Document;
using ReactiveUI;
using S7Tools.Core.Interfaces;
using S7Tools.Models.Hex;

namespace S7Tools.ViewModels.Hex
{
    public class HexViewerViewModel : ReactiveObject, IDisposable
    {
        private IBinaryDocument? _document;
        private string _fileName = string.Empty;
        private long _fileSize;
        private int _displayBase = 16;
        private long _selectionStart;
        private long _selectionLength;

        public HexViewerViewModel()
        {
            CloseFileCommand = ReactiveCommand.Create(CloseFile);

            this.WhenAnyValue(x => x.SelectionStart, x => x.SelectionLength)
                .Subscribe(_ => UpdateInspector());

            // Initialize DataInspector and wire up events
            DataInspector = new DataInspectorViewModel();
            DataInspector.RequestFillSelection += OnRequestFillSelection;
            DataInspector.RequestGoToOffset += OnRequestGoToOffset;
        }

        public event Action<long>? NavigateTo;

        private void OnRequestGoToOffset(long offset)
        {
            NavigateTo?.Invoke(offset);
        }

        private void OnRequestFillSelection(byte[] pattern)
        {
            if (Document == null || SelectionLength <= 0)
                return;

            // Simple fill: Repeat pattern over the selection
            // We need to implement write logic.
            // Check if document supports writing.
            if (Document is not IBinaryDocument doc)
                return;

            try
            {
                // Create full buffer
                var length = (int)SelectionLength;
                var buffer = new byte[length];
                for (int i = 0; i < length; i++)
                {
                    buffer[i] = pattern[i % pattern.Length];
                }

                // Write
                doc.WriteBytes((ulong)SelectionStart, buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fill Error: {ex.Message}");
            }
        }

        public DataInspectorViewModel? DataInspector { get; set; }

        public IBinaryDocument? Document
        {
            get => _document;
            private set => this.RaiseAndSetIfChanged(ref _document, value);
        }

        public string FileName
        {
            get => _fileName;
            private set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        public long FileSize
        {
            get => _fileSize;
            private set => this.RaiseAndSetIfChanged(ref _fileSize, value);
        }

        public bool IsFileOpen => Document != null;

        public int DisplayBase
        {
            get => _displayBase;
            set => this.RaiseAndSetIfChanged(ref _displayBase, value);
        }

        public long SelectionStart
        {
            get => _selectionStart;
            set => this.RaiseAndSetIfChanged(ref _selectionStart, value);
        }

        public long SelectionLength
        {
            get => _selectionLength;
            set => this.RaiseAndSetIfChanged(ref _selectionLength, value);
        }

        // View Configuration
        private int _bytesPerLine = 16;
        public int BytesPerLine
        {
            get => _bytesPerLine;
            set => this.RaiseAndSetIfChanged(ref _bytesPerLine, value);
        }

        private double _fontSize = 14;
        public double FontSize
        {
            get => _fontSize;
            set => this.RaiseAndSetIfChanged(ref _fontSize, value);
        }

        private double _columnPadding = 15;
        public double ColumnPadding
        {
            get => _columnPadding;
            set => this.RaiseAndSetIfChanged(ref _columnPadding, value);
        }

        private bool _isHeaderVisible = true;
        public bool IsHeaderVisible
        {
            get => _isHeaderVisible;
            set => this.RaiseAndSetIfChanged(ref _isHeaderVisible, value);
        }

        // Column Visibility
        private bool _isOffsetColumnVisible = true;
        public bool IsOffsetColumnVisible
        {
            get => _isOffsetColumnVisible;
            set => this.RaiseAndSetIfChanged(ref _isOffsetColumnVisible, value);
        }

        private bool _isHexColumnVisible = true;
        public bool IsHexColumnVisible
        {
            get => _isHexColumnVisible;
            set => this.RaiseAndSetIfChanged(ref _isHexColumnVisible, value);
        }

        private bool _isAsciiColumnVisible = true;
        public bool IsAsciiColumnVisible
        {
            get => _isAsciiColumnVisible;
            set => this.RaiseAndSetIfChanged(ref _isAsciiColumnVisible, value);
        }

        private bool _isBinaryColumnVisible = false;
        public bool IsBinaryColumnVisible
        {
            get => _isBinaryColumnVisible;
            set => this.RaiseAndSetIfChanged(ref _isBinaryColumnVisible, value);
        }

        // Header Visibility
        private bool _isOffsetHeaderVisible = true;
        public bool IsOffsetHeaderVisible
        {
            get => _isOffsetHeaderVisible;
            set => this.RaiseAndSetIfChanged(ref _isOffsetHeaderVisible, value);
        }

        private bool _isHexHeaderVisible = true;
        public bool IsHexHeaderVisible
        {
            get => _isHexHeaderVisible;
            set => this.RaiseAndSetIfChanged(ref _isHexHeaderVisible, value);
        }

        private bool _isAsciiHeaderVisible = true;
        public bool IsAsciiHeaderVisible
        {
            get => _isAsciiHeaderVisible;
            set => this.RaiseAndSetIfChanged(ref _isAsciiHeaderVisible, value);
        }

        private bool _isBinaryHeaderVisible = true;
        public bool IsBinaryHeaderVisible
        {
            get => _isBinaryHeaderVisible;
            set => this.RaiseAndSetIfChanged(ref _isBinaryHeaderVisible, value);
        }

        public ReactiveCommand<Unit, Unit> CloseFileCommand { get; }
        public ReactiveCommand<Unit, Unit> CopyCommand { get; set; } = null!; // Set by View or initialized later if we move logic here

        public void OpenStream(string path)
        {
            CloseFile();

            try
            {
                var doc = new FileBinaryDocument(path);
                Document = doc;
                FileName = Path.GetFileName(path);
                FileSize = (long)doc.Length;

                this.RaisePropertyChanged(nameof(IsFileOpen));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening hex view: {ex.Message}");
            }
        }

        private void CloseFile()
        {
            if (Document != null)
            {
                Document.Dispose();
                Document = null;
            }
            FileName = string.Empty;
            FileSize = 0;
            this.RaisePropertyChanged(nameof(IsFileOpen));
        }

        private void UpdateInspector()
        {
            if (Document == null || SelectionLength <= 0 || DataInspector == null)
            {
                DataInspector?.Update(null);
                return;
            }

            try
            {
                // Read up to 8 bytes for inspection (since we only show up to double/64-bit)
                int count = (int)Math.Min(SelectionLength, 8);
                var buffer = new byte[count];
                Document.ReadBytes((ulong)SelectionStart, buffer);
                DataInspector.Update(buffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating inspector: {ex.Message}");
            }
        }

        public void Dispose()
        {
            CloseFile();
        }
    }
}
