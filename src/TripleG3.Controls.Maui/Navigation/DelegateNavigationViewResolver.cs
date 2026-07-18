namespace TripleG3.Controls.Maui.Navigation;

public sealed class DelegateNavigationViewResolver<TState>(
    Func<State<TState>, CancellationToken, ValueTask<View?>> resolveAsync) : INavigationViewResolver<TState>
{
    private readonly Func<State<TState>, CancellationToken, ValueTask<View?>> resolveAsync =
        resolveAsync ?? throw new ArgumentNullException(nameof(resolveAsync));

    public ValueTask<View?> ResolveAsync(
        State<TState> state,
        CancellationToken cancellationToken = default) =>
        resolveAsync(state, cancellationToken);
}
