using System;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;

namespace S7Tools.Services;

/// <summary>
/// Design-time implementation of IViewModelFactory for XAML designer support.
/// </summary>
internal class DesignTimeViewModelFactory : IViewModelFactory
{
    /// <inheritdoc/>
    public T Create<T>() where T : ViewModelBase
    {
        return (T)Activator.CreateInstance(typeof(T))!;
    }

    /// <inheritdoc/>
    public ViewModelBase Create(Type viewModelType)
    {
        return (ViewModelBase)Activator.CreateInstance(viewModelType)!;
    }
}
