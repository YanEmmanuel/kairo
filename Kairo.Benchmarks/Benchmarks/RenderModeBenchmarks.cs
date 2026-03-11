using BenchmarkDotNet.Attributes;
using Kairo.Core.Models;
using Kairo.Rendering.Modes;

namespace Kairo.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public sealed class RenderModeBenchmarks
{
    private readonly AsciiRenderMode _ascii = new();
    private readonly BlocksRenderMode _blocks = new();
    private readonly BrailleRenderMode _braille = new();
    private VideoFrame _asciiFrame = null!;
    private VideoFrame _blocksFrame = null!;
    private VideoFrame _brailleFrame = null!;
    private TerminalFrameBuffer _asciiBuffer = null!;
    private TerminalFrameBuffer _blocksBuffer = null!;
    private TerminalFrameBuffer _brailleBuffer = null!;
    private Kairo.Core.Playback.ResolvedPlaybackSettings _asciiSettings = null!;
    private Kairo.Core.Playback.ResolvedPlaybackSettings _blocksSettings = null!;
    private Kairo.Core.Playback.ResolvedPlaybackSettings _brailleSettings = null!;

    [GlobalSetup]
    public void Setup()
    {
        _asciiSettings = BenchmarkFrameFactory.CreateSettings(Kairo.Core.Rendering.RenderModeKind.Ascii, 120, 40);
        _blocksSettings = BenchmarkFrameFactory.CreateSettings(Kairo.Core.Rendering.RenderModeKind.Blocks, 120, 40);
        _brailleSettings = BenchmarkFrameFactory.CreateSettings(Kairo.Core.Rendering.RenderModeKind.Braille, 120, 40);

        _asciiFrame = BenchmarkFrameFactory.CreateFrame(_asciiSettings.Layout.ScaledWidth, _asciiSettings.Layout.ScaledHeight);
        _blocksFrame = BenchmarkFrameFactory.CreateFrame(_blocksSettings.Layout.ScaledWidth, _blocksSettings.Layout.ScaledHeight);
        _brailleFrame = BenchmarkFrameFactory.CreateFrame(_brailleSettings.Layout.ScaledWidth, _brailleSettings.Layout.ScaledHeight);

        _asciiBuffer = new TerminalFrameBuffer(120, 40);
        _blocksBuffer = new TerminalFrameBuffer(120, 40);
        _brailleBuffer = new TerminalFrameBuffer(120, 40);
    }

    [Benchmark]
    public void Ascii()
    {
        _ascii.Render(_asciiFrame, _asciiSettings.Layout, _asciiBuffer, _asciiSettings);
    }

    [Benchmark]
    public void Blocks()
    {
        _blocks.Render(_blocksFrame, _blocksSettings.Layout, _blocksBuffer, _blocksSettings);
    }

    [Benchmark]
    public void Braille()
    {
        _braille.Render(_brailleFrame, _brailleSettings.Layout, _brailleBuffer, _brailleSettings);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _asciiFrame.Dispose();
        _blocksFrame.Dispose();
        _brailleFrame.Dispose();
        _asciiBuffer.Dispose();
        _blocksBuffer.Dispose();
        _brailleBuffer.Dispose();
    }
}
