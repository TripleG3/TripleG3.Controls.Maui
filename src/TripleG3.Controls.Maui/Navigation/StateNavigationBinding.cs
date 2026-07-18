namespace TripleG3.Controls.Maui.Navigation;

public sealed class StateNavigationBinding<TState> : IStateNavigationBinding
{
    private readonly IStateService<TState> stateService;
    private readonly INavigationViewResolver<TState> viewResolver;
    private readonly INavigationCoordinator coordinator;
    private readonly Func<State<TState>, object?> identitySelector;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private int started;
    private int disposed;

    public event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

    public StateNavigationBinding(
        IStateService<TState> stateService,
        INavigationViewResolver<TState> viewResolver,
        INavigationCoordinator coordinator,
        Func<State<TState>, object?>? identitySelector = null)
    {
        this.stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        this.viewResolver = viewResolver ?? throw new ArgumentNullException(nameof(viewResolver));
        this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        this.identitySelector = identitySelector ?? (static state => state.Value);
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        if (Interlocked.Exchange(ref started, 1) != 0)
        {
            return;
        }

        stateService.StateChanged += OnStateChanged;
        QueueState(stateService.State);
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        stateService.StateChanged -= OnStateChanged;
        lifetimeCancellation.Cancel();
        lifetimeCancellation.Dispose();
        return ValueTask.CompletedTask;
    }

    private void OnStateChanged(object? sender, State<TState> state) => QueueState(state);

    private void QueueState(State<TState> state)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            return;
        }

        _ = ResolveAndQueueAsync(state, lifetimeCancellation.Token);
    }

    private async Task ResolveAndQueueAsync(State<TState> state, CancellationToken cancellationToken)
    {
        try
        {
            View? view = await viewResolver.ResolveAsync(state, cancellationToken).ConfigureAwait(false);
            if (view is null)
            {
                return;
            }

            NavigationViewRequest request = new(identitySelector(state), view);
            await coordinator.EnqueueAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            NavigationFailed?.Invoke(this, new NavigationFailedEventArgs(exception, state));
        }
    }
}
