using Kairo.Core.Playback;
using Kairo.Video.FFmpeg;

namespace Kairo.Tests.Video;

public sealed class FFplayArgumentBuilderTests
{
    [Fact]
    public void BuildAudioPlayback_MapsLoopAndTrimFlags()
    {
        var args = FFplayArgumentBuilder.BuildAudioPlayback(
            new AudioPlaybackRequest("/tmp/demo file.mp4", TimeSpan.FromSeconds(12.5d), TimeSpan.FromSeconds(5), true));

        Assert.Contains("-loop 0", args);
        Assert.Contains("-ss 12.5", args);
        Assert.Contains("-t 5", args);
        Assert.Contains("\"/tmp/demo file.mp4\"", args);
        Assert.Contains("-nodisp", args);
        Assert.DoesNotContain("-nostdin", args);
        Assert.DoesNotContain("-dn", args);
    }
}
