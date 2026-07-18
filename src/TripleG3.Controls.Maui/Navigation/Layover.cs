namespace TripleG3.Controls.Maui.Navigation;

public sealed class Layover : ContentView, ILayover
{
    private readonly Grid overlay;

    public Layover()
    {
        InputTransparent = true;
        IsVisible = false;
        Opacity = 0d;
        overlay = new Grid
        {
            BackgroundColor = Colors.Transparent
        };
        base.Content = overlay;
    }

    public new View? Content
    {
        get => overlay.Children.FirstOrDefault() as View;
        set
        {
            overlay.Children.Clear();
            if (value is not null)
            {
                overlay.Children.Add(value);
            }
        }
    }

    public bool IsPresented => IsVisible;

    public async ValueTask ShowAsync(View content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        await DispatchAsync(async () =>
        {
            Content = content;
            InputTransparent = false;
            IsVisible = true;
            Opacity = 0d;
            await this.FadeToAsync(1d, 160u, Easing.CubicOut)
                .WaitAsync(cancellationToken);
        }).ConfigureAwait(false);
    }

    public async ValueTask HideAsync(CancellationToken cancellationToken = default)
    {
        await DispatchAsync(async () =>
        {
            if (!IsVisible)
            {
                return;
            }

            await this.FadeToAsync(0d, 120u, Easing.CubicIn)
                .WaitAsync(cancellationToken);
            IsVisible = false;
            InputTransparent = true;
            Content = null;
        }).ConfigureAwait(false);
    }

    private Task DispatchAsync(Func<Task> action)
    {
        if (Dispatcher.IsDispatchRequired)
        {
            return Dispatcher.DispatchAsync(action);
        }

        return action();
    }
}
