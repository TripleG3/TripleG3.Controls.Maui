namespace TripleG3.Controls.Maui.Audio;

public interface IRealtimeAudioVisualizationService : IRealtimeAudioSpectrumSource, IRealtimeAudioVisualizationSink, IDisposable
{
    void Start();

    void Stop();
}