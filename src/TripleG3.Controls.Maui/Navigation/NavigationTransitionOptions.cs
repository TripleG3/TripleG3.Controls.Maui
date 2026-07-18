namespace TripleG3.Controls.Maui.Navigation;

public sealed record NavigationTransitionOptions
{
    public static NavigationTransitionOptions Default { get; } = new();

    public TimeSpan NavigateOutDuration { get; init; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan NavigateInDuration { get; init; } = TimeSpan.FromMilliseconds(500);

    public TimeSpan NavigateOutDelay { get; init; } = TimeSpan.Zero;

    public TimeSpan NavigateInDelay { get; init; } = TimeSpan.FromMilliseconds(200);

    public NavigationTransitionMode Mode { get; init; } = NavigationTransitionMode.Concurrent;

    public NavigationSlideDirection NavigateOutDirection { get; init; } = NavigationSlideDirection.Left;

    public NavigationSlideDirection NavigateInDirection { get; init; } = NavigationSlideDirection.Right;

    public double SlideDistance { get; init; } = 72d;

    public bool Fade { get; init; } = true;

    public Easing NavigateOutEasing { get; init; } = Easing.CubicIn;

    public Easing NavigateInEasing { get; init; } = Easing.CubicOut;

    internal void Validate()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(NavigateOutDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(NavigateInDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(NavigateOutDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(NavigateInDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegative(SlideDistance);
        ArgumentNullException.ThrowIfNull(NavigateOutEasing);
        ArgumentNullException.ThrowIfNull(NavigateInEasing);
    }
}
