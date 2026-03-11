using System.Globalization;
using System.Text;
using Kairo.Core.Playback;

namespace Kairo.Video.FFmpeg;

internal static class FFplayArgumentBuilder
{
    public static string BuildAudioPlayback(AudioPlaybackRequest request)
    {
        var builder = new StringBuilder(160);
        builder.Append("-v error -hide_banner -nostats -nodisp -autoexit -vn -sn ");

        if (request.Loop)
        {
            builder.Append("-loop 0 ");
        }

        if (request.StartAt > TimeSpan.Zero)
        {
            builder.Append("-ss ");
            builder.Append(FormatSeconds(request.StartAt));
            builder.Append(' ');
        }

        if (request.Duration is not null)
        {
            builder.Append("-t ");
            builder.Append(FormatSeconds(request.Duration.Value));
            builder.Append(' ');
        }

        builder.Append('"');
        builder.Append(Escape(request.InputPath));
        builder.Append('"');
        return builder.ToString();
    }

    private static string FormatSeconds(TimeSpan value) =>
        value.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
