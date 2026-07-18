namespace TripleG3.Controls.Maui.Navigation;

public sealed class MauiNavigationTransition : INavigationTransition
{
    public async ValueTask NavigateOutAsync(
        View currentView,
        NavigationTransitionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentView);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        await DelayAsync(options.NavigateOutDelay, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        double targetX = GetHorizontalOffset(options.NavigateOutDirection, options.SlideDistance);
        double targetY = GetVerticalOffset(options.NavigateOutDirection, options.SlideDistance);
        Task movement = currentView.TranslateToAsync(targetX, targetY, ToMilliseconds(options.NavigateOutDuration), options.NavigateOutEasing);
        Task fade = options.Fade
            ? currentView.FadeToAsync(0d, ToMilliseconds(options.NavigateOutDuration), options.NavigateOutEasing)
            : Task.CompletedTask;

        await Task.WhenAll(movement, fade).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask NavigateInAsync(
        View newView,
        NavigationTransitionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newView);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        newView.TranslationX = GetHorizontalOffset(options.NavigateInDirection, options.SlideDistance);
        newView.TranslationY = GetVerticalOffset(options.NavigateInDirection, options.SlideDistance);
        newView.Opacity = options.Fade ? 0d : 1d;

        await DelayAsync(options.NavigateInDelay, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        Task movement = newView.TranslateToAsync(0d, 0d, ToMilliseconds(options.NavigateInDuration), options.NavigateInEasing);
        Task fade = options.Fade
            ? newView.FadeToAsync(1d, ToMilliseconds(options.NavigateInDuration), options.NavigateInEasing)
            : Task.CompletedTask;

        await Task.WhenAll(movement, fade).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        delay > TimeSpan.Zero ? Task.Delay(delay, cancellationToken) : Task.CompletedTask;

    private static uint ToMilliseconds(TimeSpan duration) =>
        checked((uint)Math.Ceiling(duration.TotalMilliseconds));

    private static double GetHorizontalOffset(NavigationSlideDirection direction, double distance) => direction switch
    {
        NavigationSlideDirection.Left => -distance,
        NavigationSlideDirection.Right => distance,
        _ => 0d
    };

    private static double GetVerticalOffset(NavigationSlideDirection direction, double distance) => direction switch
    {
        NavigationSlideDirection.Up => -distance,
        NavigationSlideDirection.Down => distance,
        _ => 0d
    };
}
