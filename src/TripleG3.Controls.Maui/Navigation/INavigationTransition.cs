namespace TripleG3.Controls.Maui.Navigation;

public interface INavigationTransition
{
    ValueTask NavigateOutAsync(
        View currentView,
        NavigationTransitionOptions options,
        CancellationToken cancellationToken = default);

    ValueTask NavigateInAsync(
        View newView,
        NavigationTransitionOptions options,
        CancellationToken cancellationToken = default);
}
