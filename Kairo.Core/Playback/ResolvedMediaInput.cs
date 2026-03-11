namespace Kairo.Core.Playback;

public sealed record ResolvedMediaInput(
    string OriginalInput,
    string PlaybackPath,
    bool IsRemote,
    bool DownloadedFromRemote);
