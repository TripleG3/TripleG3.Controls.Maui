namespace TripleG3.Controls.Maui.Navigation;

public interface INavigationCoordinator : IAsyncDisposable
{
    event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

    View? CurrentView { get; }

    bool IsNavigating { get; }

    ValueTask EnqueueAsync(
        INavigationRequest request,
        CancellationToken cancellationToken = default);

    ValueTask WhenIdleAsync(CancellationToken cancellationToken = default);
}
