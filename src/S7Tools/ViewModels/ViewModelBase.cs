using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace S7Tools.ViewModels;

/// <summary>
/// Base class for all view models.
/// </summary>
public class ViewModelBase : ReactiveObject
{
    /// <summary>
    /// Updates an ObservableCollection efficiently by minimizing UI updates.
    /// Only adds new items and removes items that are no longer present.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    /// <param name="collection">The observable collection to update.</param>
    /// <param name="newItems">The new list of items.</param>
    protected static void UpdateObservableCollection<T>(ObservableCollection<T> collection, IEnumerable<T> newItems)
    {
        var newItemsSet = new HashSet<T>(newItems);

        // Remove items that are no longer present
        for (int i = collection.Count - 1; i >= 0; i--)
        {
            if (!newItemsSet.Contains(collection[i]))
            {
                collection.RemoveAt(i);
            }
        }

        var currentItemsSet = new HashSet<T>(collection);
        // Add new items that aren't already in the collection
        foreach (T? item in newItemsSet)
        {
            if (!currentItemsSet.Contains(item))
            {
                collection.Add(item);
            }
        }
    }
}
