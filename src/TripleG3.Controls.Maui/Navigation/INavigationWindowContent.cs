namespace TripleG3.Controls.Maui.Navigation;

public interface INavigationWindowContent
{
    Navigator Navigator { get; }

    ILayover Layover { get; }

    View View { get; }
}
