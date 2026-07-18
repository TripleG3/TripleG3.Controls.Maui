namespace TripleG3.Controls.Maui.Audio;

public interface IRealtimeAudioVisualizationSink
{
    void PublishPcm16(ReadOnlySpan<byte> audio, int sampleRate, int channels);

    void Clear();
}
