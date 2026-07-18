using System.Threading.Channels;

namespace TripleG3.Controls.Maui.Audio;

public sealed class RealtimeAudioVisualizationService : IRealtimeAudioVisualizationService
{
    private readonly Lock gate = new();
    private Channel<QueuedPcm16Audio> audioSegments = CreateChannel();
    private CancellationTokenSource? cancellation;
    private Task? processingTask;
    private bool isRunning;
    private int disposed;

    public event EventHandler<RealtimeAudioSpectrumFrame>? SpectrumAvailable;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        lock (gate)
        {
            if (isRunning) return;
            audioSegments = CreateChannel();
            cancellation = new CancellationTokenSource();
            processingTask = ProcessAudioAsync(audioSegments.Reader, cancellation.Token);
            isRunning = true;
        }
    }

    public void Stop()
    {
        Task? task;
        CancellationTokenSource? tokenSource;
        lock (gate)
        {
            if (!isRunning) return;
            isRunning = false;
            audioSegments.Writer.TryComplete();
            task = processingTask;
            processingTask = null;
            tokenSource = cancellation;
            cancellation = null;
        }
        tokenSource?.Cancel();
        try { task?.Wait(TimeSpan.FromMilliseconds(500)); }
        catch (AggregateException exception) when (exception.InnerExceptions.All(static error => error is OperationCanceledException)) { }
        finally { tokenSource?.Dispose(); }
    }

    public void PublishPcm16(ReadOnlySpan<byte> audio, int sampleRate, int channels)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);
        if (audio.IsEmpty) return;
        Channel<QueuedPcm16Audio> channel;
        lock (gate)
        {
            if (!isRunning) return;
            channel = audioSegments;
        }
        channel.Writer.TryWrite(new QueuedPcm16Audio(audio.ToArray(), sampleRate, channels));
    }

    public void Clear() => Publish(RealtimeAudioSpectrumFrame.CreateSilence());

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0) Stop();
    }

    private async Task ProcessAudioAsync(ChannelReader<QueuedPcm16Audio> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (QueuedPcm16Audio audio in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                Publish(RealtimeAudioSpectrumAnalyzer.AnalyzePcm16(audio.Audio, audio.SampleRate, audio.Channels));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void Publish(RealtimeAudioSpectrumFrame frame) => SpectrumAvailable?.Invoke(this, frame);

    private static Channel<QueuedPcm16Audio> CreateChannel() => Channel.CreateBounded<QueuedPcm16Audio>(new BoundedChannelOptions(16) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false, AllowSynchronousContinuations = false });

    private sealed record QueuedPcm16Audio(byte[] Audio, int SampleRate, int Channels);
}
