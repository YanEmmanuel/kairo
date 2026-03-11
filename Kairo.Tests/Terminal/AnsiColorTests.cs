using Kairo.Core.Models;
using Kairo.Terminal.Ansi;

namespace Kairo.Tests.Terminal;

public sealed class AnsiColorTests
{
    [Fact]
    public void AppendForeground_UsesTruecolorEscape()
    {
        using var builder = new PooledAnsiBuilder();
        builder.AppendForeground(new Rgb24(10, 20, 30));

        Assert.Equal("\u001b[38;2;10;20;30m", builder.WrittenSpan.ToString());
    }
}
