namespace Kairo.Core.Models;

public readonly record struct VideoMetadata(
    int Width,
    int Height,
    double FrameRate,
    TimeSpan Duration,
    long? FrameCount,
    string PixelFormat);
