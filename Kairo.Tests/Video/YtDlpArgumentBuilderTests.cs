using Kairo.Video.FFmpeg;

namespace Kairo.Tests.Video;

public sealed class YtDlpArgumentBuilderTests
{
    [Fact]
    public void BuildDownload_IncludesBundledRuntimeHints()
    {
        var args = YtDlpArgumentBuilder.BuildDownload(
            "https://example.com/watch?v=demo",
            "/tmp/kairo",
            "/opt/kairo/tools",
            "/opt/kairo/tools/deno");

        Assert.Contains("--ffmpeg-location", args);
        Assert.Contains("/opt/kairo/tools", args);
        Assert.Contains("--js-runtimes", args);
        Assert.Contains("deno:/opt/kairo/tools/deno", args);
    }
}
