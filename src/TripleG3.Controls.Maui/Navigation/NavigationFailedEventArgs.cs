namespace TripleG3.Controls.Maui.Navigation;

public sealed class NavigationFailedEventArgs(
    Exception exception,
    object? context = null) : EventArgs
{
    public Exception Exception { get; } = exception ?? throw new ArgumentNullException(nameof(exception));

    public object? Context { get; } = context;
}
