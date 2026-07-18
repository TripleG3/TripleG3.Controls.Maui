namespace TripleG3.Controls.Maui.Navigation;

public interface IStateNavigationBinding : IAsyncDisposable
{
    event EventHandler<NavigationFailedEventArgs>? NavigationFailed;

    void Start();
}
