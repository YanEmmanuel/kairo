using Kairo.Video.FFmpeg;

namespace Kairo.Tests.Video;

public sealed class YtDlpMediaInputResolverTests : IDisposable
{
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), $"kairo-tests-{Guid.NewGuid():N}");

    [Fact]
    public void LooksLikeRemoteInput_DetectsHttpAndHttps()
    {
        Assert.True(YtDlpMediaInputResolver.LooksLikeRemoteInput("https://example.com/video"));
        Assert.True(YtDlpMediaInputResolver.LooksLikeRemoteInput("http://example.com/video"));
        Assert.False(YtDlpMediaInputResolver.LooksLikeRemoteInput("/tmp/video.mp4"));
    }

    [Fact]
    public void TryResolveDownloadedPath_ReturnsExistingFileFromYtDlpOutput()
    {
        Directory.CreateDirectory(_tempDirectory);
        var expectedPath = Path.Combine(_tempDirectory, "clip [abc123].mp4");
        File.WriteAllText(expectedPath, "stub");

        var output = $"""
                      {_tempDirectory}/clip [abc123].mp4
                      """;

        var resolvedPath = YtDlpMediaInputResolver.TryResolveDownloadedPath(output, _tempDirectory);

        Assert.Equal(expectedPath, resolvedPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
