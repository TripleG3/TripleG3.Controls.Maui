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
        if (IsNoOp(options.NavigateOutDelay, options.NavigateOutDuration, options.SlideDistance, options.Fade))
        {
            return;
        }

        await DelayAsync(options.NavigateOutDelay, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        double targetX = GetHorizontalOffset(options.NavigateOutDirection, options.SlideDistance);
        double targetY = GetVerticalOffset(options.NavigateOutDirection, options.SlideDistance);
        Task animation = await StartAnimationAsync(currentView, () =>
        {
            Task movement = currentView.TranslateToAsync(targetX, targetY, ToMilliseconds(options.NavigateOutDuration), options.NavigateOutEasing);
            Task fade = options.Fade
                ? currentView.FadeToAsync(0d, ToMilliseconds(options.NavigateOutDuration), options.NavigateOutEasing)
                : Task.CompletedTask;

            return Task.WhenAll(movement, fade);
        }).ConfigureAwait(false);
        await animation.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask NavigateInAsync(
        View newView,
        NavigationTransitionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newView);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (IsNoOp(options.NavigateInDelay, options.NavigateInDuration, options.SlideDistance, options.Fade))
        {
            return;
        }

        await DispatchAsync(newView, () =>
        {
            newView.TranslationX = GetHorizontalOffset(options.NavigateInDirection, options.SlideDistance);
            newView.TranslationY = GetVerticalOffset(options.NavigateInDirection, options.SlideDistance);
            newView.Opacity = options.Fade ? 0d : 1d;
        }).ConfigureAwait(false);

        await DelayAsync(options.NavigateInDelay, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        Task animation = await StartAnimationAsync(newView, () =>
        {
            Task movement = newView.TranslateToAsync(0d, 0d, ToMilliseconds(options.NavigateInDuration), options.NavigateInEasing);
            Task fade = options.Fade
                ? newView.FadeToAsync(1d, ToMilliseconds(options.NavigateInDuration), options.NavigateInEasing)
                : Task.CompletedTask;

            return Task.WhenAll(movement, fade);
        }).ConfigureAwait(false);
        await animation.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Task> StartAnimationAsync(View view, Func<Task> startAnimation)
    {
        if (view.Dispatcher.IsDispatchRequired)
        {
            TaskCompletionSource<Task> animationSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            await view.Dispatcher.DispatchAsync(() =>
            {
                try
                {
                    animationSource.TrySetResult(startAnimation());
                }
                catch (Exception exception)
                {
                    animationSource.TrySetException(exception);
                }
            }).ConfigureAwait(false);
            return await animationSource.Task.ConfigureAwait(false);
        }

        return startAnimation();
    }

    private static Task DispatchAsync(View view, Action action)
    {
        if (view.Dispatcher.IsDispatchRequired)
        {
            return view.Dispatcher.DispatchAsync(action);
        }

        action();
        return Task.CompletedTask;
    }

    private static bool IsNoOp(TimeSpan delay, TimeSpan duration, double slideDistance, bool fade) =>
        delay == TimeSpan.Zero && duration == TimeSpan.Zero && slideDistance == 0d && !fade;

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
