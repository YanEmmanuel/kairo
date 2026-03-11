using Kairo.Core.Models;
using Kairo.Core.Rendering;

namespace Kairo.Core.Playback;

public static class AutoPresetSelector
{
    public static RenderModeKind ResolveMode(PlaybackOptions options, TerminalSize terminal)
    {
        if (options.Mode is not RenderModeKind.Auto)
        {
            return options.Mode;
        }

        if (!options.ColorEnabled)
        {
            return RenderModeKind.Ascii;
        }

        if (options.Detail is DetailLevel.Ultra or DetailLevel.Insane && terminal.Area >= 4_200)
        {
            return RenderModeKind.Braille;
        }

        if (terminal.Area < 1_200)
        {
            return RenderModeKind.Ascii;
        }

        return RenderModeKind.Blocks;
    }

    public static int ResolveBufferSize(PlaybackOptions options, DetailLevel detail) =>
        options.BufferSize ?? detail switch
        {
            DetailLevel.Fast => 2,
            DetailLevel.Balanced => 3,
            DetailLevel.Quality => 4,
            DetailLevel.Ultra => 5,
            DetailLevel.Insane => 12,
            _ => 3
        };

    public static int ResolveStartupBufferFrames(PlaybackOptions options, DetailLevel detail, int bufferSize)
    {
        if (options.PreviewFrame || options.MaxFps || options.Benchmark || bufferSize <= 1)
        {
            return 0;
        }

        return detail switch
        {
            DetailLevel.Insane => Math.Min(bufferSize, Math.Max(3, (bufferSize * 3 + 3) / 4)),
            _ => 0
        };
    }

    public static double ResolveTargetFps(PlaybackOptions options, VideoMetadata metadata)
    {
        if (options.Fps is > 0)
        {
            return options.Fps.Value;
        }

        if (metadata.FrameRate > 0.1d)
        {
            return metadata.FrameRate;
        }

        return 24d;
    }

    public static ModeDescriptor ResolveDescriptor(PlaybackOptions options, RenderModeKind mode) =>
        mode switch
        {
            RenderModeKind.Blocks when options.Detail is DetailLevel.Quality or DetailLevel.Ultra or DetailLevel.Insane
                => new ModeDescriptor(mode, 2, 2),
            _ => ModeCatalog.GetDescriptor(mode)
        };

    public static string ResolveScaleAlgorithm(DetailLevel detail) =>
        detail switch
        {
            DetailLevel.Fast => "fast_bilinear",
            DetailLevel.Balanced => "bilinear",
            DetailLevel.Quality => "bicubic",
            DetailLevel.Ultra => "lanczos",
            DetailLevel.Insane => "lanczos",
            _ => "bilinear"
        };

    public static bool ShouldPreferSmoothPlayback(PlaybackOptions options) =>
        options.Audio == AudioMode.Off &&
        !options.MaxFps &&
        !options.Benchmark &&
        !options.PreviewFrame &&
        options.Detail == DetailLevel.Insane;

    public static bool ShouldTrackResize(PlaybackOptions options) =>
        !options.Width.HasValue || !options.Height.HasValue;
}
