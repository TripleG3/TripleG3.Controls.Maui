namespace TripleG3.Controls.Maui.Audio;

public sealed class RealtimeAudioWaveformControl : GraphicsView
{
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(IRealtimeAudioSpectrumSource), typeof(RealtimeAudioWaveformControl), propertyChanged: OnSourceChanged);
    public static readonly BindableProperty StrokeProperty = BindableProperty.Create(nameof(Stroke), typeof(Color), typeof(RealtimeAudioWaveformControl), Colors.DeepSkyBlue, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty GlowStrokeProperty = BindableProperty.Create(nameof(GlowStroke), typeof(Color), typeof(RealtimeAudioWaveformControl), Colors.MediumOrchid, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(nameof(StrokeThickness), typeof(float), typeof(RealtimeAudioWaveformControl), 0.5f, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty GlowThicknessProperty = BindableProperty.Create(nameof(GlowThickness), typeof(float), typeof(RealtimeAudioWaveformControl), 1f, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty IdleAmplitudeProperty = BindableProperty.Create(nameof(IdleAmplitude), typeof(double), typeof(RealtimeAudioWaveformControl), 0.1d, propertyChanged: OnAppearanceChanged);
    public static readonly BindableProperty EdgeFadePercentProperty = BindableProperty.Create(nameof(EdgeFadePercent), typeof(double), typeof(RealtimeAudioWaveformControl), 0.23d, propertyChanged: OnAppearanceChanged);

    private readonly WaveformDrawable waveformDrawable;
    private bool isRendering;

    public RealtimeAudioWaveformControl()
    {
        waveformDrawable = new WaveformDrawable(this);
        Drawable = waveformDrawable;
        HeightRequest = 56;
        WidthRequest = 360;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public IRealtimeAudioSpectrumSource? Source { get => (IRealtimeAudioSpectrumSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public Color Stroke { get => (Color)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }
    public Color GlowStroke { get => (Color)GetValue(GlowStrokeProperty); set => SetValue(GlowStrokeProperty, value); }
    public float StrokeThickness { get => (float)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }
    public float GlowThickness { get => (float)GetValue(GlowThicknessProperty); set => SetValue(GlowThicknessProperty, value); }
    public double IdleAmplitude { get => (double)GetValue(IdleAmplitudeProperty); set => SetValue(IdleAmplitudeProperty, value); }
    public double EdgeFadePercent { get => (double)GetValue(EdgeFadePercentProperty); set => SetValue(EdgeFadePercentProperty, value); }

    private static void OnSourceChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        RealtimeAudioWaveformControl control = (RealtimeAudioWaveformControl)bindable;
        if (oldValue is IRealtimeAudioSpectrumSource oldSource) oldSource.SpectrumAvailable -= control.OnSpectrumAvailable;
        if (control.IsLoaded && newValue is IRealtimeAudioSpectrumSource newSource) newSource.SpectrumAvailable += control.OnSpectrumAvailable;
    }

    private static void OnAppearanceChanged(BindableObject bindable, object? oldValue, object? newValue) => ((RealtimeAudioWaveformControl)bindable).Invalidate();

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (Source is not null) Source.SpectrumAvailable += OnSpectrumAvailable;
        StartRendering();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (Source is not null) Source.SpectrumAvailable -= OnSpectrumAvailable;
        StopRendering();
    }

    private void OnSpectrumAvailable(object? sender, RealtimeAudioSpectrumFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        Dispatcher.Dispatch(() => waveformDrawable.ApplyFrame(frame));
    }

    private void StartRendering()
    {
        if (isRendering) return;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (!isRendering) return false;
            waveformDrawable.Advance();
            Invalidate();
            return true;
        });
        isRendering = true;
    }

    private void StopRendering() => isRendering = false;

    private sealed class WaveformDrawable(RealtimeAudioWaveformControl control) : IDrawable
    {
        private static readonly TimeSpan FrameHoldDuration = TimeSpan.FromMilliseconds(180);
        private double[] currentMagnitudes = [];
        private double[] targetMagnitudes = [];
        private double[] magnitudeVelocities = [];
        private double[] currentWaveformSamples = [];
        private double[] targetWaveformSamples = [];
        private double[] waveformVelocities = [];
        private DateTimeOffset lastFrameUtc = DateTimeOffset.MinValue;
        private double currentLoudness, targetLoudness, loudnessVelocity, currentPeak, targetPeak, peakVelocity, phase;

        public void ApplyFrame(RealtimeAudioSpectrumFrame frame)
        {
            targetMagnitudes = [.. frame.Magnitudes.Select(ShapeMagnitudeTarget)];
            targetWaveformSamples = [.. frame.WaveformSamples.Select(ShapeWaveformTarget)];
            EnsureBuffers();
            targetLoudness = ShapeMagnitudeTarget(frame.Loudness);
            targetPeak = ShapeMagnitudeTarget(frame.Peak);
            lastFrameUtc = frame.TimestampUtc;
            control.Invalidate();
        }

        public void Advance()
        {
            EnsureBuffers();
            bool recent = DateTimeOffset.UtcNow - lastFrameUtc <= FrameHoldDuration;
            double average = 0;
            for (int i = 0; i < targetMagnitudes.Length; i++)
            {
                if (!recent) targetMagnitudes[i] *= 0.86d;
                currentMagnitudes[i] = Step(currentMagnitudes[i], targetMagnitudes[i], ref magnitudeVelocities[i], .34, .095, .66, 0, 1.12);
                average += currentMagnitudes[i];
            }
            for (int i = 0; i < targetWaveformSamples.Length; i++)
            {
                if (!recent) targetWaveformSamples[i] *= .82d;
                currentWaveformSamples[i] = Step(currentWaveformSamples[i], targetWaveformSamples[i], ref waveformVelocities[i], .42, .12, .64, -1.12, 1.12);
            }
            if (!recent) { targetLoudness *= .84d; targetPeak *= .84d; }
            currentLoudness = Step(currentLoudness, targetLoudness, ref loudnessVelocity, .4, .11, .64, 0, 1.12);
            currentPeak = Step(currentPeak, targetPeak, ref peakVelocity, .38, .1, .64, 0, 1.12);
            phase = (phase + .08d + ((average / Math.Max(1, currentMagnitudes.Length)) * .28d) + (currentLoudness * .46d)) % (Math.PI * 2d);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (dirtyRect.Width <= 1 || dirtyRect.Height <= 1) return;
            PointF[] points = CreatePoints(dirtyRect);
            DrawWave(canvas, points, control.GlowStroke, Math.Max(0, control.GlowThickness));
            DrawWave(canvas, points, control.Stroke, Math.Max(.5f, control.StrokeThickness));
        }

        private void DrawWave(ICanvas canvas, IReadOnlyList<PointF> points, Color color, float thickness)
        {
            if (points.Count < 2 || thickness <= 0) return;
            canvas.StrokeSize = thickness;
            for (int i = 0; i < points.Count - 1; i++)
            {
                float t = i / (float)(points.Count - 2);
                float opacity = EdgeOpacity(t);
                canvas.StrokeColor = color.WithAlpha(color.Alpha * opacity);
                canvas.DrawLine(points[i], points[i + 1]);
            }
        }

        private PointF[] CreatePoints(RectF rect)
        {
            int count = Math.Max(48, Math.Max(currentMagnitudes.Length, currentWaveformSamples.Length));
            PointF[] points = new PointF[count];
            double baseline = rect.Top + (rect.Height * .54d);
            double maxAmplitude = Math.Max(2, rect.Height * .47d);
            double padding = Math.Max(control.StrokeThickness + 1, 2);
            for (int i = 0; i < count; i++)
            {
                double t = count == 1 ? 0 : i / (double)(count - 1);
                double intensity = Interpolate(currentMagnitudes, t);
                double sample = Interpolate(currentWaveformSamples, t);
                double center = .12d + (Math.Pow(Math.Sin(t * Math.PI), .72d) * 1.38d);
                double live = Math.Pow(Math.Clamp(Math.Max(currentLoudness, Math.Max(currentPeak * .72d, intensity * .86d)), 0, 1), .72d);
                double carrier = (Math.Sin((t * Math.PI * 5.25d) + phase) * .68d) + (Math.Sin((t * Math.PI * 12d) - (phase * .72d)) * .32d);
                double idle = Math.Clamp(control.IdleAmplitude, 0, .4d) * Math.Exp(-Math.Pow((t - .5d) / .26d, 2)) * (.35d + (.65d * Math.Abs(Math.Sin((i * .62d) + (phase * .35d)))));
                double y = baseline - (sample * live * maxAmplitude * center) - (carrier * intensity * maxAmplitude * .36d * center) - (carrier * idle * maxAmplitude * (1 - (live * .72d)) * center);
                points[i] = new PointF((float)(rect.Left + (t * rect.Width)), (float)Math.Clamp(y, rect.Top + padding, rect.Bottom - padding));
            }
            return points;
        }

        private float EdgeOpacity(float t)
        {
            double fade = Math.Clamp(control.EdgeFadePercent, 0, .49d);
            if (fade == 0) return 1;
            return (float)Math.Clamp(Math.Min(t / fade, (1 - t) / fade), 0, 1);
        }

        private void EnsureBuffers()
        {
            if (currentMagnitudes.Length != targetMagnitudes.Length) currentMagnitudes = new double[targetMagnitudes.Length];
            if (magnitudeVelocities.Length != targetMagnitudes.Length) magnitudeVelocities = new double[targetMagnitudes.Length];
            if (currentWaveformSamples.Length != targetWaveformSamples.Length) currentWaveformSamples = new double[targetWaveformSamples.Length];
            if (waveformVelocities.Length != targetWaveformSamples.Length) waveformVelocities = new double[targetWaveformSamples.Length];
        }

        private static double Interpolate(double[] values, double t)
        {
            if (values.Length == 0) return 0;
            double position = t * (values.Length - 1); int lower = Math.Clamp((int)Math.Floor(position), 0, values.Length - 1); int upper = Math.Min(lower + 1, values.Length - 1);
            return (values[lower] * (1 - (position - lower))) + (values[upper] * (position - lower));
        }

        private static double ShapeMagnitudeTarget(double value) => Math.Clamp(Math.Pow(Math.Clamp(value, 0, 1), .72d) * (1 + (.18d * Math.Sin(Math.Clamp(value, 0, 1) * Math.PI))), 0, 1);
        private static double ShapeWaveformTarget(double value) => Math.Sign(value) * Math.Clamp(Math.Pow(Math.Clamp(Math.Abs(value), 0, 1), .82d) * (1 + (.14d * Math.Sin(Math.Clamp(Math.Abs(value), 0, 1) * Math.PI))), 0, 1);
        private static double Step(double current, double target, ref double velocity, double attack, double release, double damping, double min, double max)
        {
            velocity = ((target - current) * ((Math.Abs(target) > Math.Abs(current) || (target != 0 && Math.Sign(target) != Math.Sign(current))) ? attack : release)) + (velocity * damping);
            double next = current + velocity;
            if (next < min || next > max) { next = Math.Clamp(next, min, max); velocity *= -.18d; }
            return next;
        }
    }
}