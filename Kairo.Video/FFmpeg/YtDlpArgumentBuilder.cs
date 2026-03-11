using System.Text;

namespace Kairo.Video.FFmpeg;

internal static class YtDlpArgumentBuilder
{
    public static string BuildDownload(string inputUrl, string outputDirectory, string? ffmpegLocation = null, string? denoPath = null)
    {
        var builder = new StringBuilder(384);
        builder.Append("--no-playlist --no-part --no-progress --no-warnings --newline ");
        builder.Append("--restrict-filenames ");
        builder.Append("-f \"bv*+ba/b\" ");
        builder.Append("--merge-output-format mp4 ");

        if (!string.IsNullOrWhiteSpace(ffmpegLocation))
        {
            builder.Append("--ffmpeg-location \"");
            builder.Append(Escape(ffmpegLocation));
            builder.Append("\" ");
        }

        if (!string.IsNullOrWhiteSpace(denoPath))
        {
            builder.Append("--js-runtimes \"deno:");
            builder.Append(Escape(denoPath));
            builder.Append("\" ");
        }

        builder.Append("-P \"");
        builder.Append(Escape(outputDirectory));
        builder.Append("\" ");
        builder.Append("-o \"%(title).120B [%(id)s].%(ext)s\" ");
        builder.Append("--print before_dl:filepath ");
        builder.Append("--print after_move:filepath ");
        builder.Append("-- \"");
        builder.Append(Escape(inputUrl));
        builder.Append('"');
        return builder.ToString();
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
