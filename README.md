# TripleG3.Controls.Maui
Controls For Maui Applications

## Custom Navigation

`TripleG3.Controls.Maui.Navigation` provides a Shell-free, state-driven navigation stack:

- `NavigationWindowContent` composes the window-level `Navigator` and `Layover` layers.
- `Navigator` is the only navigation UI control and keeps an incoming view above the outgoing view while transitions overlap.
- `NavigationCoordinator` queues every request and completes each transition before starting the next. Enqueueing never waits for an active animation.
- `StateNavigationBinding<TState>` immediately resolves the current `IStateService<TState>.State`, subscribes to `StateChanged`, and enqueues each resolved destination.
- `INavigationViewResolver<TState>` maps immutable CIS state snapshots to views without coupling the control library to an application.
- `INavigationTransition` owns `NavigateOutAsync` and `NavigateInAsync`. `MauiNavigationTransition` supports concurrent, out-then-in, and in-then-out sequencing, independent offsets, duration, fading, easing, and slide direction.
- `ILayover` exposes a separate overlay for progress, waiting, and confirmation content.

Create a `Window` whose content is `INavigationWindowContent.View`, construct a `NavigationCoordinator` for its `Navigator`, then start a `StateNavigationBinding<TState>`. Dispose the binding and coordinator with the owning window or application scope.

Navigation identity is separate from the view instance. Supply a stable identity from state when a resolver creates fresh views so repeat snapshots do not animate the same destination out and back in.
