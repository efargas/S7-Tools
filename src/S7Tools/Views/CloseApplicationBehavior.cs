using System;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using ReactiveUI;

namespace S7Tools.Views;

/// <summary>
/// A behavior that closes the associated window when an interaction is received.
/// </summary>
public class CloseApplicationBehavior : Behavior<Window>, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public static readonly StyledProperty<Interaction<Unit, Unit>?> InteractionProperty =
        AvaloniaProperty.Register<CloseApplicationBehavior, Interaction<Unit, Unit>?>(nameof(Interaction));
    /// <summary>
    /// Gets or sets the interaction to listen for.
    /// </summary>
    public Interaction<Unit, Unit>? Interaction
    {
        get => GetValue(InteractionProperty);
        set => SetValue(InteractionProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        // We need to subscribe to changes of the Interaction property,
        // in case it is set after the behavior is attached.
        this.GetObservable(InteractionProperty).Subscribe(interaction =>
        {
            _disposables.Clear();
            if (interaction is not null && AssociatedObject is not null) // Check for null to satisfy the compiler and for safety
            {
                interaction.RegisterHandler(ctx => { AssociatedObject.Close(); ctx.SetOutput(Unit.Default); })
                    .DisposeWith(_disposables);
            }
        }).DisposeWith(_disposables);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        _disposables.Dispose();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
