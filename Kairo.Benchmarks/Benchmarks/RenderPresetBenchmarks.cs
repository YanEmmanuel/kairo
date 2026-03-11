using BenchmarkDotNet.Attributes;
using Kairo.Core.Models;
using Kairo.Core.Rendering;
using Kairo.Rendering.Modes;

namespace Kairo.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public sealed class RenderPresetBenchmarks
{
    private readonly BlocksRenderMode _renderer = new();
    private VideoFrame _frame = null!;
    private TerminalFrameBuffer _buffer = null!;
    private Kairo.Core.Playback.ResolvedPlaybackSettings _settings = null!;

    [Params(DetailLevel.Fast, DetailLevel.Balanced, DetailLevel.Quality, DetailLevel.Ultra, DetailLevel.Insane)]
    public DetailLevel Detail { get; set; }

    [IterationSetup]
    public void Setup()
    {
        var width = Detail switch
        {
            DetailLevel.Fast => 80,
            DetailLevel.Balanced => 120,
            DetailLevel.Quality => 140,
            DetailLevel.Ultra => 160,
            _ => 180
        };

        var height = Detail switch
        {
            DetailLevel.Fast => 30,
            DetailLevel.Balanced => 40,
            DetailLevel.Quality => 45,
            DetailLevel.Ultra => 50,
            _ => 54
        };

        _settings = BenchmarkFrameFactory.CreateSettings(RenderModeKind.Blocks, width, height);
        _frame = BenchmarkFrameFactory.CreateFrame(_settings.Layout.ScaledWidth, _settings.Layout.ScaledHeight);
        _buffer = new TerminalFrameBuffer(width, height);
    }

    [Benchmark]
    public void BlocksAtDetailLevel()
    {
        _renderer.Render(_frame, _settings.Layout, _buffer, _settings);
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _frame.Dispose();
        _buffer.Dispose();
    }
}
