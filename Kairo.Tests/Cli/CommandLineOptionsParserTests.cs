using Kairo.Cli.Cli;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;

namespace Kairo.Tests.Cli;

public sealed class CommandLineOptionsParserTests
{
    [Fact]
    public void Parse_MapsCommonFlags()
    {
        var result = CommandLineOptionsParser.Parse(
            [
                "video.mp4",
                "--mode", "blocks",
                "--fps", "30",
                "--width", "120",
                "--height", "40",
                "--crop",
                "--no-color",
                "--detail", "ultra",
                "--buffer-size", "5",
                "--start-at", "12.5",
                "--duration", "5"
            ]);

        Assert.False(result.HasError);
        Assert.NotNull(result.Options);
        Assert.Equal("video.mp4", result.Options!.InputPath);
        Assert.Equal(RenderModeKind.Blocks, result.Options.Mode);
        Assert.Equal(30d, result.Options.Fps);
        Assert.Equal(120, result.Options.Width);
        Assert.Equal(40, result.Options.Height);
        Assert.Equal(ResizeMode.Crop, result.Options.ResizeMode);
        Assert.False(result.Options.ColorEnabled);
        Assert.Equal(DetailLevel.Ultra, result.Options.Detail);
        Assert.Equal(5, result.Options.BufferSize);
        Assert.Equal(TimeSpan.FromSeconds(12.5d), result.Options.StartAt);
        Assert.Equal(TimeSpan.FromSeconds(5d), result.Options.Duration);
    }

    [Fact]
    public void Parse_RejectsUnknownOption()
    {
        var result = CommandLineOptionsParser.Parse(["video.mp4", "--wat"]);
        Assert.True(result.HasError);
        Assert.Contains("Unknown option", result.ErrorMessage);
    }

    [Fact]
    public void Parse_MapsAudioFlag()
    {
        var result = CommandLineOptionsParser.Parse(["https://youtu.be/demo", "--audio", "on"]);

        Assert.False(result.HasError);
        Assert.NotNull(result.Options);
        Assert.Equal("https://youtu.be/demo", result.Options!.InputPath);
        Assert.Equal(AudioMode.On, result.Options.Audio);
    }

    [Fact]
    public void Parse_AllowsAudioToOverrideMuteCompatibilityFlag()
    {
        var result = CommandLineOptionsParser.Parse(["video.mp4", "--mute", "--audio", "on"]);

        Assert.False(result.HasError);
        Assert.NotNull(result.Options);
        Assert.Equal(AudioMode.On, result.Options!.Audio);
    }

    [Fact]
    public void Parse_RejectsUnsupportedAudioMode()
    {
        var result = CommandLineOptionsParser.Parse(["video.mp4", "--audio", "maybe"]);

        Assert.True(result.HasError);
        Assert.Contains("Unsupported audio mode", result.ErrorMessage);
    }
}
