using TripleG3.Controls.Maui.Navigation;

namespace TripleG3.Controls.Maui.Tests.Navigation;

public sealed class AsyncNavigationQueueTests
{
    [Fact]
    public async Task EnqueueAsync_DoesNotWaitForCurrentNavigation()
    {
        TaskCompletionSource started = NewCompletion();
        TaskCompletionSource release = NewCompletion();
        await using AsyncNavigationQueue<string> queue = new(async (item, cancellationToken) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
        });

        ValueTask enqueue = queue.EnqueueAsync("home");

        Assert.True(enqueue.IsCompletedSuccessfully);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        release.TrySetResult();
        await queue.WhenIdleAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task ItemsQueuedDuringNavigation_AreProcessedInOrder()
    {
        List<string> processed = [];
        TaskCompletionSource firstStarted = NewCompletion();
        TaskCompletionSource releaseFirst = NewCompletion();
        await using AsyncNavigationQueue<string> queue = new(async (item, cancellationToken) =>
        {
            processed.Add(item);
            if (item == "home")
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task.WaitAsync(cancellationToken);
            }
        });

        await queue.EnqueueAsync("home");
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await queue.EnqueueAsync("settings");
        await queue.EnqueueAsync("profile");
        releaseFirst.TrySetResult();
        await queue.WhenIdleAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(["home", "settings", "profile"], processed);
    }

    [Fact]
    public async Task ProcessingFailure_IsReportedAndNextItemContinues()
    {
        List<string> processed = [];
        List<NavigationFailedEventArgs> failures = [];
        await using AsyncNavigationQueue<string> queue = new((item, _) =>
        {
            if (item == "failed")
            {
                throw new InvalidOperationException("Expected transition failure.");
            }

            processed.Add(item);
            return ValueTask.CompletedTask;
        });
        queue.ProcessingFailed += (_, args) => failures.Add(args);

        await queue.EnqueueAsync("failed");
        await queue.EnqueueAsync("recovered");
        await queue.WhenIdleAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Single(failures);
        Assert.Equal("failed", failures[0].Context);
        Assert.Equal(["recovered"], processed);
    }

    [Fact]
    public async Task CanceledQueuedItem_IsSkipped()
    {
        List<string> processed = [];
        TaskCompletionSource firstStarted = NewCompletion();
        TaskCompletionSource releaseFirst = NewCompletion();
        await using AsyncNavigationQueue<string> queue = new(async (item, cancellationToken) =>
        {
            processed.Add(item);
            if (item == "home")
            {
                firstStarted.TrySetResult();
                await releaseFirst.Task.WaitAsync(cancellationToken);
            }
        });
        using CancellationTokenSource canceled = new();

        await queue.EnqueueAsync("home");
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await queue.EnqueueAsync("settings", canceled.Token);
        canceled.Cancel();
        releaseFirst.TrySetResult();
        await queue.WhenIdleAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(["home"], processed);
    }

    private static TaskCompletionSource NewCompletion() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}