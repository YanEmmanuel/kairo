namespace Kairo.Core.Playback;

public sealed record AudioPlaybackRequest(
    string InputPath,
    TimeSpan StartAt,
    TimeSpan? Duration,
    bool Loop);
