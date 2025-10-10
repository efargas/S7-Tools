using System;
using Microsoft.Extensions.DependencyInjection;
using S7Tools.Services.Interfaces;
using S7Tools.ViewModels;

namespace S7Tools.Services;

/// <summary>
/// Factory for creating ViewModels through dependency injection.
/// </summary>
public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewModelFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public T Create<T>() where T : ViewModelBase
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <inheritdoc/>
    public ViewModelBase Create(Type viewModelType)
    {
        if (!typeof(ViewModelBase).IsAssignableFrom(viewModelType))
        {
            throw new ArgumentException($"Type {viewModelType.Name} must inherit from ViewModelBase", nameof(viewModelType));
        }

        return (ViewModelBase)_serviceProvider.GetRequiredService(viewModelType);
    }
}
