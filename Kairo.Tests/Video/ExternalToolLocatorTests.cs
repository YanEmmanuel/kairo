using Kairo.Video.FFmpeg;

namespace Kairo.Tests.Video;

public sealed class ExternalToolLocatorTests
{
    [Fact]
    public void EnumerateCandidatePaths_PrefersOverrideThenBundledToolsThenBaseDirectory()
    {
        var candidates = ExternalToolLocator
            .EnumerateCandidatePaths(
                "ffmpeg",
                "/app",
                "/custom/ffmpeg",
                "/bundle/tools",
                string.Join(Path.PathSeparator, "/usr/bin", "/bin"))
            .Take(5)
            .ToArray();

        Assert.Equal("/custom/ffmpeg", candidates[0]);
        Assert.Equal(Path.Combine("/bundle/tools", GetPlatformFileName("ffmpeg")), candidates[1]);
        Assert.Equal(Path.Combine("/app", GetPlatformFileName("ffmpeg")), candidates[2]);
        Assert.Equal(Path.Combine("/usr/bin", GetPlatformFileName("ffmpeg")), candidates[3]);
        Assert.Equal(Path.Combine("/bin", GetPlatformFileName("ffmpeg")), candidates[4]);
    }

    private static string GetPlatformFileName(string toolName) =>
        OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName;
}
