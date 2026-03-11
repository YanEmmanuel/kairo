using Kairo.Core.Rendering;

namespace Kairo.Core.Playback;

public sealed class PlaybackOptions
{
    public string InputPath { get; init; } = string.Empty;

    public string PlaybackPath =>
        string.IsNullOrWhiteSpace(ResolvedInputPath)
            ? InputPath
            : ResolvedInputPath;

    public string? ResolvedInputPath { get; set; }

    public RenderModeKind Mode { get; init; } = RenderModeKind.Auto;

    public double? Fps { get; init; }

    public bool MaxFps { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public ResizeMode ResizeMode { get; init; } = ResizeMode.Fit;

    public string Charset { get; init; } = " .,:;irsXA253hMHGS#9B&@";

    public bool InvertCharset { get; init; }

    public bool ColorEnabled { get; init; } = true;

    public DetailLevel Detail { get; init; } = DetailLevel.Balanced;

    public double Brightness { get; init; }

    public double Contrast { get; init; } = 1d;

    public double Saturation { get; init; } = 1d;

    public double Gamma { get; init; } = 1d;

    public bool Loop { get; init; }

    public AudioMode Audio { get; init; } = AudioMode.Off;

    public bool Benchmark { get; init; }

    public bool Stats { get; init; }

    public bool DisableDiff { get; init; }

    public bool ForceFullRedraw { get; init; }

    public int? Threads { get; init; }

    public int? BufferSize { get; init; }

    public string EmojiStyle { get; init; } = "meme";

    public DitherMode Dither { get; init; } = DitherMode.None;

    public bool Profile { get; init; }

    public string? SaveFramesPath { get; init; }

    public string? ExportGifPath { get; init; }

    public string? ExportVideoPath { get; init; }

    public bool PreviewFrame { get; init; }

    public TimeSpan StartAt { get; init; } = TimeSpan.Zero;

    public TimeSpan? Duration { get; init; }

    public bool ShowHelp { get; init; }

    public bool ShowVersion { get; init; }

    public bool HasUnsupportedExportRequest =>
        !string.IsNullOrWhiteSpace(SaveFramesPath) ||
        !string.IsNullOrWhiteSpace(ExportGifPath) ||
        !string.IsNullOrWhiteSpace(ExportVideoPath);
}
