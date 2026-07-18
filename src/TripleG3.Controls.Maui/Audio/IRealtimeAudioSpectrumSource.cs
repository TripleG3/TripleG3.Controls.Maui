namespace TripleG3.Controls.Maui.Audio;

public interface IRealtimeAudioSpectrumSource
{
    event EventHandler<RealtimeAudioSpectrumFrame>? SpectrumAvailable;
}
