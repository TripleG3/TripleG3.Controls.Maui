namespace TripleG3.Controls.Maui.Audio;

public sealed class RealtimeAudioSpectrumFrame
{
    public RealtimeAudioSpectrumFrame(
        IEnumerable<double> magnitudes,
        int sampleRate,
        int minFrequencyHz,
        int maxFrequencyHz,
        int bandWidthHz,
        DateTimeOffset timestampUtc,
        IEnumerable<double>? waveformSamples = null,
        double loudness = 0,
        double peak = 0)
    {
        ArgumentNullException.ThrowIfNull(magnitudes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minFrequencyHz);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFrequencyHz, minFrequencyHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bandWidthHz);

        Magnitudes = [.. magnitudes.Select(static value => Math.Clamp(value, 0d, 1d))];
        WaveformSamples = [.. (waveformSamples ?? []).Select(static value => Math.Clamp(value, -1d, 1d))];
        Loudness = Math.Clamp(loudness, 0d, 1d);
        Peak = Math.Clamp(peak, 0d, 1d);
        SampleRate = sampleRate;
        MinFrequencyHz = minFrequencyHz;
        MaxFrequencyHz = maxFrequencyHz;
        BandWidthHz = bandWidthHz;
        TimestampUtc = timestampUtc;
    }

    public IReadOnlyList<double> Magnitudes { get; }
    public IReadOnlyList<double> WaveformSamples { get; }
    public double Loudness { get; }
    public double Peak { get; }
    public int SampleRate { get; }
    public int MinFrequencyHz { get; }
    public int MaxFrequencyHz { get; }
    public int BandWidthHz { get; }
    public DateTimeOffset TimestampUtc { get; }

    public int GetFrequencyForBand(int bandIndex)
    {
        if (bandIndex < 0 || bandIndex >= Magnitudes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(bandIndex));
        }

        return MinFrequencyHz + (bandIndex * BandWidthHz);
    }

    public static RealtimeAudioSpectrumFrame CreateSilence(
        int sampleRate = RealtimeAudioVisualizationDefaults.SampleRate,
        int minFrequencyHz = RealtimeAudioVisualizationDefaults.MinimumFrequencyHz,
        int maxFrequencyHz = RealtimeAudioVisualizationDefaults.MaximumFrequencyHz,
        int bandWidthHz = RealtimeAudioVisualizationDefaults.BandWidthHz) =>
        new(new double[RealtimeAudioSpectrumAnalyzer.GetBandCount(minFrequencyHz, maxFrequencyHz, bandWidthHz)], sampleRate, minFrequencyHz, maxFrequencyHz, bandWidthHz, DateTimeOffset.UtcNow, new double[RealtimeAudioSpectrumAnalyzer.DisplayWaveformSampleCount]);
}