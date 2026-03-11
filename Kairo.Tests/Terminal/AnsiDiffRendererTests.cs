using Kairo.Core.Models;
using Kairo.Terminal.Ansi;

namespace Kairo.Tests.Terminal;

public sealed class AnsiDiffRendererTests
{
    [Fact]
    public void Render_OutputsOnlyChangedCells()
    {
        using var previous = new TerminalFrameBuffer(2, 1);
        using var current = new TerminalFrameBuffer(2, 1);
        using var builder = new PooledAnsiBuilder();

        previous.GetCellRef(0, 0) = new TerminalCell('A', Rgb24.White, Rgb24.Black);
        previous.GetCellRef(1, 0) = new TerminalCell('B', Rgb24.White, Rgb24.Black);

        current.GetCellRef(0, 0) = new TerminalCell('A', Rgb24.White, Rgb24.Black);
        current.GetCellRef(1, 0) = new TerminalCell('C', new Rgb24(255, 0, 0), Rgb24.Black);

        AnsiDiffRenderer.Render(current, previous, builder, forceFullRedraw: false);
        var output = builder.WrittenSpan.ToString();

        Assert.DoesNotContain("[1;1H", output);
        Assert.Contains("[1;2H", output);
        Assert.Contains("38;2;255;0;0m", output);
        Assert.Contains("C", output);
    }
}
