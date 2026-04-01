using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;
using AvaloniaHex.Document;
using ReactiveUI;
using S7Tools.Services.Hex;

namespace S7Tools.ViewModels.Hex
{
    /// <summary>
    /// Represents the SearchMode.
    /// </summary>
    public enum SearchMode
    {
        Hex,
        String,
        Binary // Interpreted as bit string "0101"
    }

    /// <summary>
    /// Represents the SearchViewModel.
    /// </summary>
    public class SearchViewModel : ReactiveObject
    {
        private readonly IBinarySearchService _searchService;
        private string _queryText = string.Empty;
        private SearchMode _searchMode = SearchMode.Hex;
        private string _statusMessage = string.Empty;
        private bool _isBusy;
        private IBinaryDocument? _document;
        private int _currentResultIndex = -1;
        private ObservableCollection<long> _searchResults = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchViewModel"/> class.
        /// </summary>
        public SearchViewModel(IBinarySearchService searchService)
        {
            _searchService = searchService;

            FindCommand = ReactiveCommand.CreateFromTask(ExecuteFind,
                this.WhenAnyValue(x => x.QueryText, x => x.IsBusy, (q, b) => !string.IsNullOrWhiteSpace(q) && !b));

            FindNextCommand = ReactiveCommand.Create(ExecuteFindNext,
                 this.WhenAnyValue(x => x.SearchResults.Count, c => c > 0));

            FindPreviousCommand = ReactiveCommand.Create(ExecuteFindPrevious,
                 this.WhenAnyValue(x => x.SearchResults.Count, c => c > 0));

            CloseCommand = ReactiveCommand.Create(() => { IsVisible = false; });
        }

        /// <summary>
        /// Gets or sets the FindCommand.
        /// </summary>
        public ReactiveCommand<Unit, Unit> FindCommand { get; }
        /// <summary>
        /// Gets or sets the FindNextCommand.
        /// </summary>
        public ReactiveCommand<Unit, Unit> FindNextCommand { get; }
        /// <summary>
        /// Gets or sets the FindPreviousCommand.
        /// </summary>
        public ReactiveCommand<Unit, Unit> FindPreviousCommand { get; }
        /// <summary>
        /// Gets or sets the CloseCommand.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        public event Action<long>? RequestNavigation;

        private bool _isVisible;
        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public string QueryText
        {
            get => _queryText;
            set => this.RaiseAndSetIfChanged(ref _queryText, value);
        }

        public SearchMode SearchMode
        {
            get => _searchMode;
            set => this.RaiseAndSetIfChanged(ref _searchMode, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        public ObservableCollection<long> SearchResults
        {
            get => _searchResults;
            set => this.RaiseAndSetIfChanged(ref _searchResults, value);
        }

        public int CurrentResultIndex
        {
            get => _currentResultIndex;
            set
            {
                if (_currentResultIndex != value)
                {
                    this.RaiseAndSetIfChanged(ref _currentResultIndex, value);
                    NavigateToCurrent();
                }
            }
        }

        /// <summary>
        /// Executes the SetDocument operation.
        /// </summary>
        public void SetDocument(IBinaryDocument? document)
        {
            _document = document;
            SearchResults.Clear();
            CurrentResultIndex = -1;
            StatusMessage = string.Empty;
        }

        private async Task ExecuteFind(CancellationToken ct)
        {
            if (_document == null)
            {
                return;
            }

            IsBusy = true;
            StatusMessage = "Searching...";
            SearchResults.Clear();
            CurrentResultIndex = -1;

            try
            {
                byte[]? pattern = ParsePattern(QueryText, SearchMode);
                if (pattern == null || pattern.Length == 0)
                {
                    StatusMessage = "Invalid pattern.";
                    return;
                }

                IEnumerable<long> results = await _searchService.FindAllAsync(_document, pattern, ct);

                foreach (long res in results)
                {
                    SearchResults.Add(res);
                }

                if (SearchResults.Count > 0)
                {
                    StatusMessage = $"Found {SearchResults.Count} matches.";
                    CurrentResultIndex = 0;
                    NavigateToCurrent();
                }
                else
                {
                    StatusMessage = "No matches found.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteFindNext()
        {
            if (SearchResults.Count == 0)
            {
                return;
            }

            CurrentResultIndex++;
            if (CurrentResultIndex >= SearchResults.Count)
            {
                CurrentResultIndex = 0; // Wrap around
            }

            NavigateToCurrent();
        }

        private void ExecuteFindPrevious()
        {
            if (SearchResults.Count == 0)
            {
                return;
            }

            CurrentResultIndex--;
            if (CurrentResultIndex < 0)
            {
                CurrentResultIndex = SearchResults.Count - 1; // Wrap around
            }

            NavigateToCurrent();
        }

        private void NavigateToCurrent()
        {
            if (CurrentResultIndex >= 0 && CurrentResultIndex < SearchResults.Count)
            {
                RequestNavigation?.Invoke(SearchResults[CurrentResultIndex]);
            }
        }

        private byte[]? ParsePattern(string text, SearchMode mode)
        {
            try
            {
                switch (mode)
                {
                    case SearchMode.String:
                        return Encoding.ASCII.GetBytes(text); // Or UTF8, standardizing on ASCII for now related to hex view standard usage
                    case SearchMode.Hex:
                        // "AB CD" -> [0xAB, 0xCD]
                        // Remove spaces
                        string hex = text.Replace(" ", "").Replace("-", "");
                        if (hex.Length % 2 != 0)
                        {
                            return null; // Invalid
                        }

                        return Convert.FromHexString(hex);
                    case SearchMode.Binary:
                        // "01000001" -> byte
                        // Must be groups of 8? Or just sequence of bits?
                        // Implementing strict byte alignment for now.
                        string bin = text.Replace(" ", "");
                        if (bin.Length % 8 != 0)
                        {
                            return null;
                        }

                        var bytes = new List<byte>();
                        for (int i = 0; i < bin.Length; i += 8)
                        {
                            string chunk = bin.Substring(i, 8);
                            bytes.Add(Convert.ToByte(chunk, 2));
                        }
                        return bytes.ToArray();
                    default:
                        return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
