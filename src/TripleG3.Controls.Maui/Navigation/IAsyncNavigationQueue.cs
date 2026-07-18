namespace TripleG3.Controls.Maui.Navigation;

public interface IAsyncNavigationQueue<T> : IAsyncDisposable
{
    event EventHandler<NavigationFailedEventArgs>? ProcessingFailed;

    int PendingCount { get; }

    ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default);

    ValueTask WhenIdleAsync(CancellationToken cancellationToken = default);
}
