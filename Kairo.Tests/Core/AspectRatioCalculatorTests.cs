using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;
using Kairo.Core.Utilities;

namespace Kairo.Tests.Core;

public sealed class AspectRatioCalculatorTests
{
    [Fact]
    public void Calculate_FitModeCentersContent()
    {
        var metadata = new VideoMetadata(1920, 1080, 24d, TimeSpan.FromMinutes(1), null, "yuv420p");
        var terminal = new TerminalSize(120, 40);
        var options = new PlaybackOptions { InputPath = "video.mp4", ResizeMode = ResizeMode.Fit };

        var layout = AspectRatioCalculator.Calculate(metadata, terminal, options, ModeCatalog.GetDescriptor(RenderModeKind.Blocks));

        Assert.Equal(120, layout.TerminalWidth);
        Assert.Equal(40, layout.TerminalHeight);
        Assert.Equal(0, layout.ContentX);
        Assert.True(layout.ContentHeight < layout.TerminalHeight);
        Assert.True(layout.ContentY > 0);
    }

    [Fact]
    public void Calculate_CropModeKeepsFullTerminalSurface()
    {
        var metadata = new VideoMetadata(1920, 800, 24d, TimeSpan.FromMinutes(1), null, "yuv420p");
        var terminal = new TerminalSize(120, 40);
        var options = new PlaybackOptions { InputPath = "video.mp4", ResizeMode = ResizeMode.Crop };

        var layout = AspectRatioCalculator.Calculate(metadata, terminal, options, ModeCatalog.GetDescriptor(RenderModeKind.Ascii));

        Assert.Equal(120, layout.ContentWidth);
        Assert.Equal(40, layout.ContentHeight);
        Assert.True(layout.SourceWidth < metadata.Width);
        Assert.Equal(metadata.Height, layout.SourceHeight);
    }
}
