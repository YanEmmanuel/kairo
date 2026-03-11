using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;

namespace Kairo.Tests.Core;

public sealed class AutoPresetSelectorTests
{
    [Fact]
    public void ResolveMode_PrefersBlocksForDefaultColorPlayback()
    {
        var options = new PlaybackOptions { InputPath = "video.mp4", Detail = DetailLevel.Balanced };
        var mode = AutoPresetSelector.ResolveMode(options, new TerminalSize(120, 40));
        Assert.Equal(RenderModeKind.Blocks, mode);
    }

    [Fact]
    public void ResolveMode_PrefersBrailleForLargeUltraPlayback()
    {
        var options = new PlaybackOptions { InputPath = "video.mp4", Detail = DetailLevel.Ultra };
        var mode = AutoPresetSelector.ResolveMode(options, new TerminalSize(160, 50));
        Assert.Equal(RenderModeKind.Braille, mode);
    }

    [Fact]
    public void ResolveDescriptor_UsesQuadrantDensityForBlocksAtHighDetail()
    {
        var options = new PlaybackOptions
        {
            InputPath = "video.mp4",
            Mode = RenderModeKind.Blocks,
            Detail = DetailLevel.Quality
        };

        var descriptor = AutoPresetSelector.ResolveDescriptor(options, RenderModeKind.Blocks);

        Assert.Equal(2, descriptor.HorizontalDensity);
        Assert.Equal(2, descriptor.VerticalDensity);
    }

    [Fact]
    public void ResolveStartupBufferFrames_PrerollsInsaneDetail()
    {
        var options = new PlaybackOptions { InputPath = "video.mp4", Detail = DetailLevel.Insane };
        var bufferSize = AutoPresetSelector.ResolveBufferSize(options, options.Detail);
        var startupBufferFrames = AutoPresetSelector.ResolveStartupBufferFrames(options, options.Detail, bufferSize);

        Assert.Equal(12, bufferSize);
        Assert.Equal(9, startupBufferFrames);
    }

    [Fact]
    public void ShouldPreferSmoothPlayback_DisablesWhenAudioIsEnabled()
    {
        var options = new PlaybackOptions
        {
            InputPath = "video.mp4",
            Detail = DetailLevel.Insane,
            Audio = AudioMode.On
        };

        Assert.False(AutoPresetSelector.ShouldPreferSmoothPlayback(options));
    }
}
