namespace TripleG3.Controls.Maui.Navigation;

public interface INavigationViewResolver<TState>
{
    ValueTask<View?> ResolveAsync(
        State<TState> state,
        CancellationToken cancellationToken = default);
}
