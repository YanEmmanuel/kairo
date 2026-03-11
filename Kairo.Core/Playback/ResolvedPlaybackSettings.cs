using Kairo.Core.Models;
using Kairo.Core.Rendering;

namespace Kairo.Core.Playback;

public sealed record ResolvedPlaybackSettings(
    RenderModeKind Mode,
    ModeDescriptor Descriptor,
    ResizeMode ResizeMode,
    bool ColorEnabled,
    bool UseDiffRendering,
    bool ForceFullRedraw,
    bool MaxFps,
    double TargetFps,
    int BufferSize,
    int StartupBufferFrames,
    bool PreferSmoothPlayback,
    int? Threads,
    string Charset,
    bool InvertCharset,
    DitherMode DitherMode,
    TerminalSize TerminalSize,
    FrameLayout Layout,
    string ScaleAlgorithm,
    TimeSpan StartAt,
    TimeSpan? Duration,
    bool PreviewFrame,
    bool BenchmarkMode,
    bool EmitStats,
    bool EmitProfile,
    bool Loop,
    bool TrackResize);
