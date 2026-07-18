namespace TripleG3.Controls.Maui.Navigation;

public interface INavigationRequest
{
    object? Identity { get; }

    View View { get; }
}
