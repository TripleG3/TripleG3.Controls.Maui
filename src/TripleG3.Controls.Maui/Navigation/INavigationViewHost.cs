namespace TripleG3.Controls.Maui.Navigation;

public interface INavigationViewHost
{
    IDispatcher Dispatcher { get; }

    View? CurrentView { get; }

    void AddIncomingView(View view);

    void Commit(View? outgoingView, View incomingView);

    void RemoveUncommitted(View view);
}
