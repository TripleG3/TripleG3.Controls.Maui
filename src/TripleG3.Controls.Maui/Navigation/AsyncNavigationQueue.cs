using System.Threading.Channels;

namespace TripleG3.Controls.Maui.Navigation;

public sealed class AsyncNavigationQueue<T> : IAsyncNavigationQueue<T>
{
    private readonly Func<T, CancellationToken, ValueTask> processAsync;
    private readonly Channel<QueuedItem> items;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly Lock idleGate = new();
    private readonly Task processor;
    private TaskCompletionSource idleCompletion = CreateCompletedIdleCompletion();
    private int pendingCount;
    private int disposed;

    public AsyncNavigationQueue(Func<T, CancellationToken, ValueTask> processAsync)
    {
        this.processAsync = processAsync ?? throw new ArgumentNullException(nameof(processAsync));
        items = Channel.CreateUnbounded<QueuedItem>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = false
        });
        processor = ProcessItemsAsync(lifetimeCancellation.Token);
    }

    public event EventHandler<NavigationFailedEventArgs>? ProcessingFailed;

    public int PendingCount => Volatile.Read(ref pendingCount);

    public ValueTask EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        MarkPending();
        if (!items.Writer.TryWrite(new QueuedItem(item, cancellationToken)))
        {
            MarkCompleted();
            throw new InvalidOperationException("The navigation queue is not accepting requests.");
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask WhenIdleAsync(CancellationToken cancellationToken = default)
    {
        Task idleTask;
        lock (idleGate)
        {
            idleTask = idleCompletion.Task;
        }

        await idleTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        items.Writer.TryComplete();
        lifetimeCancellation.Cancel();
        try
        {
            await processor.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (lifetimeCancellation.IsCancellationRequested)
        {
        }
        finally
        {
            lifetimeCancellation.Dispose();
            CompleteIdle();
        }
    }

    private async Task ProcessItemsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (QueuedItem queued in items.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    if (!queued.CancellationToken.IsCancellationRequested)
                    {
                        await processAsync(queued.Item, queued.CancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException) when (queued.CancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception exception)
                {
                    ProcessingFailed?.Invoke(this, new NavigationFailedEventArgs(exception, queued.Item));
                }
                finally
                {
                    MarkCompleted();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            while (items.Reader.TryRead(out _))
            {
                MarkCompleted();
            }
        }
    }

    private void MarkPending()
    {
        lock (idleGate)
        {
            if (Interlocked.Increment(ref pendingCount) == 1)
            {
                idleCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    private void MarkCompleted()
    {
        lock (idleGate)
        {
            if (Interlocked.Decrement(ref pendingCount) <= 0)
            {
                pendingCount = 0;
                idleCompletion.TrySetResult();
            }
        }
    }

    private void CompleteIdle()
    {
        lock (idleGate)
        {
            pendingCount = 0;
            idleCompletion.TrySetResult();
        }
    }

    private static TaskCompletionSource CreateCompletedIdleCompletion()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        completion.SetResult();
        return completion;
    }

    private sealed record QueuedItem(T Item, CancellationToken CancellationToken);
}
