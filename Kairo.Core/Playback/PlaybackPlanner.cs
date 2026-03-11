using Kairo.Core.Models;
using Kairo.Core.Rendering;
using Kairo.Core.Utilities;

namespace Kairo.Core.Playback;

public static class PlaybackPlanner
{
    public static ResolvedPlaybackSettings Resolve(PlaybackOptions options, VideoMetadata metadata, TerminalSize terminalSize)
    {
        var mode = AutoPresetSelector.ResolveMode(options, terminalSize);
        var descriptor = AutoPresetSelector.ResolveDescriptor(options, mode);
        var layout = AspectRatioCalculator.Calculate(metadata, terminalSize, options, descriptor);
        var targetFps = AutoPresetSelector.ResolveTargetFps(options, metadata);
        var bufferSize = AutoPresetSelector.ResolveBufferSize(options, options.Detail);

        return new ResolvedPlaybackSettings(
            mode,
            descriptor,
            options.ResizeMode,
            options.ColorEnabled,
            !options.DisableDiff && !options.ForceFullRedraw,
            options.ForceFullRedraw,
            options.MaxFps || options.Benchmark,
            targetFps,
            bufferSize,
            AutoPresetSelector.ResolveStartupBufferFrames(options, options.Detail, bufferSize),
            AutoPresetSelector.ShouldPreferSmoothPlayback(options),
            options.Threads,
            options.Charset,
            options.InvertCharset,
            options.Dither,
            terminalSize,
            layout,
            AutoPresetSelector.ResolveScaleAlgorithm(options.Detail),
            options.StartAt,
            options.Duration,
            options.PreviewFrame,
            options.Benchmark,
            options.Stats || options.Benchmark,
            options.Profile,
            options.Loop,
            AutoPresetSelector.ShouldTrackResize(options));
    }
}
