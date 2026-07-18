using System.Buffers.Binary;

namespace TripleG3.Controls.Maui.Audio;

internal static class RealtimeAudioSpectrumAnalyzer
{
    public const int DisplayWaveformSampleCount = 48;

    public static int GetBandCount(int minFrequencyHz, int maxFrequencyHz, int bandWidthHz)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minFrequencyHz);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFrequencyHz, minFrequencyHz);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bandWidthHz);
        return ((maxFrequencyHz - minFrequencyHz) / bandWidthHz) + 1;
    }

    public static RealtimeAudioSpectrumFrame AnalyzePcm16(ReadOnlySpan<byte> audio, int sampleRate, int channels, int minFrequencyHz = RealtimeAudioVisualizationDefaults.MinimumFrequencyHz, int maxFrequencyHz = RealtimeAudioVisualizationDefaults.MaximumFrequencyHz, int bandWidthHz = RealtimeAudioVisualizationDefaults.BandWidthHz)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);
        int maximumFrequency = Math.Max(minFrequencyHz, Math.Min(maxFrequencyHz, sampleRate / 2));
        int bandCount = GetBandCount(minFrequencyHz, maximumFrequency, bandWidthHz);
        double[] samples = DecodePcm16ToMonoSamples(audio, channels);
        if (samples.Length == 0) return new RealtimeAudioSpectrumFrame(new double[bandCount], sampleRate, minFrequencyHz, maximumFrequency, bandWidthHz, DateTimeOffset.UtcNow, new double[DisplayWaveformSampleCount]);
        (double rms, double peak) = GetMetrics(samples);
        double loudness = Math.Clamp((Math.Log10(1 + (rms * 42)) * 1.1) + (Math.Log10(1 + (peak * 5)) * .35), 0, 1);
        double[] raw = new double[bandCount];
        double maximum = 0;
        for (int i = 0; i < raw.Length; i++) { raw[i] = GetBandMagnitude(samples, sampleRate, minFrequencyHz + (i * bandWidthHz)); maximum = Math.Max(maximum, raw[i]); }
        double energy = Math.Max(loudness, Math.Clamp(Math.Log10(1 + (maximum * 95)), 0, 1));
        double[] magnitudes = maximum > .000001 && energy > .001 ? [.. raw.Select(value => Math.Clamp(Math.Pow(value / maximum, .42) * energy, 0, 1))] : new double[bandCount];
        return new RealtimeAudioSpectrumFrame(magnitudes, sampleRate, minFrequencyHz, maximumFrequency, bandWidthHz, DateTimeOffset.UtcNow, CreateDisplaySamples(samples, peak), loudness, peak);
    }

    private static double[] DecodePcm16ToMonoSamples(ReadOnlySpan<byte> audio, int channels)
    {
        int frameSize = channels * sizeof(short); int count = audio.Length / frameSize;
        if (count == 0) return [];
        double[] result = new double[count];
        for (int sample = 0; sample < count; sample++)
        {
            double sum = 0;
            for (int channel = 0; channel < channels; channel++) sum += BinaryPrimitives.ReadInt16LittleEndian(audio.Slice((sample * frameSize) + (channel * sizeof(short)), sizeof(short))) / 32768d;
            result[sample] = sum / channels;
        }
        return result;
    }

    private static (double Rms, double Peak) GetMetrics(IReadOnlyList<double> samples)
    {
        double squares = 0, peak = 0;
        foreach (double sample in samples) { squares += sample * sample; peak = Math.Max(peak, Math.Abs(sample)); }
        return (Math.Sqrt(squares / samples.Count), peak);
    }

    private static double[] CreateDisplaySamples(IReadOnlyList<double> samples, double peak)
    {
        double gain = peak <= .001 ? 1 : Math.Clamp(.82 / peak, 1, 8); double[] result = new double[DisplayWaveformSampleCount];
        for (int display = 0; display < result.Length; display++)
        {
            int start = display * samples.Count / result.Length, end = Math.Max(start + 1, (display + 1) * samples.Count / result.Length); double selected = 0;
            for (int sample = start; sample < end && sample < samples.Count; sample++) if (Math.Abs(samples[sample]) > Math.Abs(selected)) selected = samples[sample];
            result[display] = Math.Clamp(selected * gain, -1, 1);
        }
        return result;
    }

    private static double GetBandMagnitude(IReadOnlyList<double> samples, int sampleRate, int frequencyHz)
    {
        double coefficient = 2 * Math.Cos(2 * Math.PI * frequencyHz / sampleRate), previous = 0, previous2 = 0;
        for (int i = 0; i < samples.Count; i++) { double window = samples.Count <= 1 ? 1 : .5 - (.5 * Math.Cos(2 * Math.PI * i / (samples.Count - 1))); double value = (samples[i] * window) + (coefficient * previous) - previous2; previous2 = previous; previous = value; }
        return Math.Clamp(Math.Log10(1 + (Math.Sqrt(Math.Max(0, (previous2 * previous2) + (previous * previous) - (coefficient * previous * previous2))) / samples.Count * 24)), 0, 1);
    }
}