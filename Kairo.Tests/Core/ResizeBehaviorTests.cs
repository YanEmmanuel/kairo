using Kairo.Core.Models;
using Kairo.Core.Playback;

namespace Kairo.Tests.Core;

public sealed class ResizeBehaviorTests
{
    [Fact]
    public void Resolve_TracksResizeWhenDimensionsAreAutomatic()
    {
        var metadata = new VideoMetadata(1280, 720, 24d, TimeSpan.FromMinutes(2), null, "yuv420p");
        var settings = PlaybackPlanner.Resolve(new PlaybackOptions { InputPath = "video.mp4" }, metadata, new TerminalSize(100, 30));

        Assert.True(settings.TrackResize);
    }

    [Fact]
    public void Resolve_DoesNotTrackResizeWhenDimensionsArePinned()
    {
        var metadata = new VideoMetadata(1280, 720, 24d, TimeSpan.FromMinutes(2), null, "yuv420p");
        var settings = PlaybackPlanner.Resolve(
            new PlaybackOptions { InputPath = "video.mp4", Width = 120, Height = 40 },
            metadata,
            new TerminalSize(100, 30));

        Assert.False(settings.TrackResize);
        Assert.Equal(120, settings.Layout.TerminalWidth);
        Assert.Equal(40, settings.Layout.TerminalHeight);
    }
}
