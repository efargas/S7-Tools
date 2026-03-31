using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace S7Tools.Collections
{
    /// <summary>
    /// An observable collection that provides bulk update capabilities via AddRange.
    /// This minimizes UI thread notifications and improves performance during high-frequency updates.
    /// </summary>
    /// <typeparam name="T">Type of items in the collection.</typeparam>
    public class FastObservableCollection<T> : ObservableCollection<T>
    {
        private bool _isAddingRange;

        /// <summary>
        /// Initializes a new instance of the <see cref="FastObservableCollection{T}"/> class.
        /// </summary>
        public FastObservableCollection() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastObservableCollection{T}"/> class that contains elements copied from the specified collection.
        /// </summary>
        /// <param name="collection">The collection from which the elements are copied.</param>
        public FastObservableCollection(IEnumerable<T> collection) : base(collection) { }

        /// <summary>
        /// Adds multiple items to the collection and raises a single NotifyCollectionChanged event.
        /// </summary>
        /// <param name="items">The items to add.</param>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _isAddingRange = true;
            try
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _isAddingRange = false;
                RaiseChangeEvents();
            }
        }

        /// <summary>
        /// Clears the collection and raises a single NotifyCollectionChanged event.
        /// </summary>
        protected override void ClearItems()
        {
            base.ClearItems();
            if (!_isAddingRange)
            {
                // base already raised events
            }
        }

        private void RaiseChangeEvents()
        {
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <inheritdoc/>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_isAddingRange)
            {
                base.OnCollectionChanged(e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_isAddingRange)
            {
                base.OnPropertyChanged(e);
            }
        }
    }
}
