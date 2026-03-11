namespace Kairo.Core.Playback;

public sealed record FrameSourceRequest(
    string InputPath,
    TimeSpan StartAt,
    TimeSpan? Duration,
    int OutputWidth,
    int OutputHeight,
    int CropX,
    int CropY,
    int CropWidth,
    int CropHeight,
    double OutputFrameRate,
    bool LimitFrameRate,
    int? Threads,
    string ScaleAlgorithm,
    double Brightness,
    double Contrast,
    double Saturation,
    double Gamma,
    PlaybackStats Stats);
