using BenchmarkDotNet.Attributes;
using Kairo.Core.Models;
using Kairo.Terminal.Ansi;

namespace Kairo.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public sealed class DiffRendererBenchmarks
{
    private TerminalFrameBuffer _previous = null!;
    private TerminalFrameBuffer _current = null!;
    private PooledAnsiBuilder _builder = null!;

    [GlobalSetup]
    public void Setup()
    {
        _previous = new TerminalFrameBuffer(120, 40);
        _current = new TerminalFrameBuffer(120, 40);
        _builder = new PooledAnsiBuilder();

        for (var y = 0; y < 40; y++)
        {
            for (var x = 0; x < 120; x++)
            {
                var color = new Rgb24((byte)(x * 2), (byte)(y * 6), (byte)((x + y) & 255));
                _previous.GetCellRef(x, y) = new TerminalCell('░', color, Rgb24.Black);
                _current.GetCellRef(x, y) = (x + y) % 9 == 0
                    ? new TerminalCell('█', color, new Rgb24(10, 10, 10))
                    : _previous.GetCellRef(x, y);
            }
        }
    }

    [Benchmark]
    public void BuildDiffBuffer()
    {
        _builder.Reset();
        AnsiDiffRenderer.Render(_current, _previous, _builder, forceFullRedraw: false);
    }

    [Benchmark]
    public void BuildFullRedrawBuffer()
    {
        _builder.Reset();
        AnsiDiffRenderer.Render(_current, _previous, _builder, forceFullRedraw: true);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _previous.Dispose();
        _current.Dispose();
        _builder.Dispose();
    }
}
