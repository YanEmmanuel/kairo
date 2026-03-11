using Kairo.Core.Playback;

namespace Kairo.Cli.Cli;

public sealed record CommandLineParseResult(PlaybackOptions? Options, string? ErrorMessage)
{
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
}
