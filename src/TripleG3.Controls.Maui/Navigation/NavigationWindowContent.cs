namespace TripleG3.Controls.Maui.Navigation;

public sealed class NavigationWindowContent : ContentView, INavigationWindowContent
{
    public NavigationWindowContent()
        : this(new Navigator(), new Layover())
    {
    }

    public NavigationWindowContent(Navigator navigator, Layover layover)
    {
        Navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        Layover = layover ?? throw new ArgumentNullException(nameof(layover));

        Grid root = new();
        root.Children.Add(Navigator);
        root.Children.Add(layover);
        Content = root;
    }

    public Navigator Navigator { get; }

    public ILayover Layover { get; }

    public View View => this;
}
