namespace TripleG3.Controls.Maui.Navigation;

public sealed record NavigationViewRequest(object? Identity, View View) : INavigationRequest
{
    public NavigationViewRequest(View view)
        : this(view, view)
    {
    }
}
