using Kairo.Rendering.Pipeline;

namespace Kairo.Tests.Rendering;

public sealed class LuminanceCharacterMapperTests
{
    [Fact]
    public void Map_UsesDarkestCharacterForLowLuminance()
    {
        var mapper = new LuminanceCharacterMapper(" .#@", invert: false);
        Assert.Equal(' ', mapper.Map(0d));
    }

    [Fact]
    public void Map_UsesBrightestCharacterForHighLuminance()
    {
        var mapper = new LuminanceCharacterMapper(" .#@", invert: false);
        Assert.Equal('@', mapper.Map(1d));
    }

    [Fact]
    public void Map_InvertsCharsetWhenRequested()
    {
        var mapper = new LuminanceCharacterMapper(" .#@", invert: true);
        Assert.Equal('@', mapper.Map(0d));
    }
}
