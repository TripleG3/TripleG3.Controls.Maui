namespace TripleG3.Controls.Maui.Navigation;

public interface ILayover
{
    View? Content { get; set; }

    bool IsPresented { get; }

    ValueTask ShowAsync(View content, CancellationToken cancellationToken = default);

    ValueTask HideAsync(CancellationToken cancellationToken = default);
}
