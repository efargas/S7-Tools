using System;
using S7Tools.ViewModels;

namespace S7Tools.Services.Interfaces;

/// <summary>
/// Factory for creating ViewModels through dependency injection.
/// </summary>
public interface IViewModelFactory
{
    /// <summary>
    /// Creates a ViewModel of the specified type.
    /// </summary>
    /// <typeparam name="T">The ViewModel type to create.</typeparam>
    /// <returns>The created ViewModel instance.</returns>
    T Create<T>() where T : ViewModelBase;

    /// <summary>
    /// Creates a ViewModel of the specified type.
    /// </summary>
    /// <param name="viewModelType">The ViewModel type to create.</param>
    /// <returns>The created ViewModel instance.</returns>
    ViewModelBase Create(Type viewModelType);
}
