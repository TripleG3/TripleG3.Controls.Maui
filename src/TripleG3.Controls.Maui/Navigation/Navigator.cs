namespace TripleG3.Controls.Maui.Navigation;

public sealed class Navigator : ContentView, INavigationViewHost
{
    private readonly Grid navigationLayer;

    public Navigator()
    {
        navigationLayer = new Grid
        {
            IsClippedToBounds = true
        };
        Content = navigationLayer;
    }

    public View? CurrentView { get; private set; }

    public void AddIncomingView(View view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (navigationLayer.Children.Contains(view))
        {
            navigationLayer.Children.Remove(view);
        }

        navigationLayer.Children.Add(view);
    }

    public void Commit(View? outgoingView, View incomingView)
    {
        ArgumentNullException.ThrowIfNull(incomingView);
        if (outgoingView is not null && !ReferenceEquals(outgoingView, incomingView))
        {
            navigationLayer.Children.Remove(outgoingView);
        }

        incomingView.Opacity = 1d;
        incomingView.TranslationX = 0d;
        incomingView.TranslationY = 0d;
        CurrentView = incomingView;
    }

    public void RemoveUncommitted(View view)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (!ReferenceEquals(CurrentView, view))
        {
            navigationLayer.Children.Remove(view);
        }
    }
}
