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

        public ReactiveCommand<Unit, Unit> CloseFileCommand { get; }

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
