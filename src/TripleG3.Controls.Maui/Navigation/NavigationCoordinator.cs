namespace TripleG3.Controls.Maui.Navigation;

public sealed class NavigationCoordinator : INavigationCoordinator
{
    private readonly INavigationViewHost navigator;
    private readonly INavigationTransition transition;
    private readonly NavigationTransitionOptions options;
    private readonly AsyncNavigationQueue<INavigationRequest> requests;
    private object? currentIdentity;
    private int isNavigating;
    private int disposed;

    public NavigationCoordinator(
        INavigationViewHost navigator,
        INavigationTransition transition,
        NavigationTransitionOptions? options = null)
    {
        this.navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        this.transition = transition ?? throw new ArgumentNullException(nameof(transition));
        this.options = options ?? NavigationTransitionOptions.Default;
        this.options.Validate();
        requests = new AsyncNavigationQueue<INavigationRequest>(NavigateAsync);
        requests.ProcessingFailed += OnRequestFailed;
    }

    public View? CurrentView => navigator.CurrentView;

    public bool IsNavigating => Volatile.Read(ref isNavigating) != 0;

    public event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

    public ValueTask EnqueueAsync(
        INavigationRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.View);
        cancellationToken.ThrowIfCancellationRequested();

        return requests.EnqueueAsync(request, cancellationToken);
    }

    public async ValueTask WhenIdleAsync(CancellationToken cancellationToken = default)
    {
        await requests.WhenIdleAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        requests.ProcessingFailed -= OnRequestFailed;
        await requests.DisposeAsync().ConfigureAwait(false);
    }

    private async ValueTask NavigateAsync(INavigationRequest request, CancellationToken cancellationToken)
    {
        if (IsSameDestination(request))
        {
            DisposeUnusedView(request.View);
            return;
        }

        Interlocked.Exchange(ref isNavigating, 1);
        View? outgoingView = navigator.CurrentView;
        View incomingView = request.View;
        try
        {
            await DispatchAsync(() => navigator.AddIncomingView(incomingView)).ConfigureAwait(false);
            await ExecuteTransitionAsync(outgoingView, incomingView, cancellationToken).ConfigureAwait(false);
            await DispatchAsync(() => navigator.Commit(outgoingView, incomingView)).ConfigureAwait(false);
            currentIdentity = request.Identity;
        }
        catch
        {
            await DispatchAsync(() => navigator.RemoveUncommitted(incomingView)).ConfigureAwait(false);
            throw;
        }
        finally
        {
            Interlocked.Exchange(ref isNavigating, 0);
        }
    }

    private Task ExecuteTransitionAsync(
        View? outgoingView,
        View incomingView,
        CancellationToken cancellationToken)
    {
        return options.Mode switch
        {
            NavigationTransitionMode.OutThenIn => ExecuteOutThenInAsync(outgoingView, incomingView, cancellationToken),
            NavigationTransitionMode.InThenOut => ExecuteInThenOutAsync(outgoingView, incomingView, cancellationToken),
            _ => ExecuteConcurrentlyAsync(outgoingView, incomingView, cancellationToken)
        };
    }

    private async Task ExecuteConcurrentlyAsync(
        View? outgoingView,
        View incomingView,
        CancellationToken cancellationToken)
    {
        Task incoming = transition.NavigateInAsync(incomingView, options, cancellationToken).AsTask();
        if (outgoingView is null)
        {
            await incoming.ConfigureAwait(false);
            return;
        }

        Task outgoing = transition.NavigateOutAsync(outgoingView, options, cancellationToken).AsTask();
        await Task.WhenAll(outgoing, incoming).ConfigureAwait(false);
    }

    private async Task ExecuteOutThenInAsync(
        View? outgoingView,
        View incomingView,
        CancellationToken cancellationToken)
    {
        if (outgoingView is not null)
        {
            await transition.NavigateOutAsync(outgoingView, options, cancellationToken).ConfigureAwait(false);
        }

        await transition.NavigateInAsync(incomingView, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteInThenOutAsync(
        View? outgoingView,
        View incomingView,
        CancellationToken cancellationToken)
    {
        await transition.NavigateInAsync(incomingView, options, cancellationToken).ConfigureAwait(false);
        if (outgoingView is not null)
        {
            await transition.NavigateOutAsync(outgoingView, options, cancellationToken).ConfigureAwait(false);
        }
    }

    private bool IsSameDestination(INavigationRequest request) =>
        ReferenceEquals(request.View, navigator.CurrentView) ||
        (request.Identity is not null && currentIdentity is not null && Equals(request.Identity, currentIdentity));

    private void DisposeUnusedView(View view)
    {
        if (!ReferenceEquals(view, navigator.CurrentView) && view is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private Task DispatchAsync(Action action)
    {
        if (navigator.Dispatcher.IsDispatchRequired)
        {
            return navigator.Dispatcher.DispatchAsync(action);
        }

        action();
        return Task.CompletedTask;
    }

    private void OnRequestFailed(object? sender, NavigationFailedEventArgs args)
    {
        object? context = args.Context is INavigationRequest request ? request.Identity : args.Context;
        NavigationFailed?.Invoke(this, new NavigationFailedEventArgs(args.Exception, context));
    }
}
