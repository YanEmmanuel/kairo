using System.Diagnostics;

namespace Kairo.Core.Playback;

public sealed class PlaybackStats
{
    private long _decodedFrames;
    private long _renderedFrames;
    private long _droppedFrames;
    private long _decodedBytes;
    private long _renderTicks;
    private long _outputTicks;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void OnFrameDecoded(int bytes)
    {
        Interlocked.Increment(ref _decodedFrames);
        Interlocked.Add(ref _decodedBytes, bytes);
    }

    public void OnFrameRendered(TimeSpan renderDuration, TimeSpan outputDuration)
    {
        Interlocked.Increment(ref _renderedFrames);
        Interlocked.Add(ref _renderTicks, renderDuration.Ticks);
        Interlocked.Add(ref _outputTicks, outputDuration.Ticks);
    }

    public void OnFrameDropped()
    {
        Interlocked.Increment(ref _droppedFrames);
    }

    public PlaybackStatsSnapshot Snapshot() =>
        new(
            _decodedFrames,
            _renderedFrames,
            _droppedFrames,
            _decodedBytes,
            _stopwatch.Elapsed,
            TimeSpan.FromTicks(_renderTicks),
            TimeSpan.FromTicks(_outputTicks));
}

public readonly record struct PlaybackStatsSnapshot(
    long DecodedFrames,
    long RenderedFrames,
    long DroppedFrames,
    long DecodedBytes,
    TimeSpan Elapsed,
    TimeSpan TotalRenderTime,
    TimeSpan TotalOutputTime)
{
    public double ActualFps => Elapsed.TotalSeconds <= 0d ? 0d : RenderedFrames / Elapsed.TotalSeconds;

    public double AverageRenderMs => RenderedFrames == 0 ? 0d : TotalRenderTime.TotalMilliseconds / RenderedFrames;

    public double AverageOutputMs => RenderedFrames == 0 ? 0d : TotalOutputTime.TotalMilliseconds / RenderedFrames;
}
