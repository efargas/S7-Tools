using System;
using System.Collections.Specialized;
using S7Tools.Core.Models;

namespace S7Tools.Core.Services.Interfaces;

public interface ITaskLogDataStore
{
    event NotifyCollectionChangedEventHandler CollectionChanged;
    void AddEntry(LogModel logModel);
    void Clear();
}
