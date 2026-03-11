using System.Globalization;
using System.Text;
using Kairo.Core.Playback;

namespace Kairo.Video.FFmpeg;

internal static class FFmpegArgumentBuilder
{
    public static string BuildProbe(string inputPath) =>
        FormattableString.Invariant(
            $"-v error -print_format json -show_streams -show_format -- \"{Escape(inputPath)}\"");

    public static string BuildDecode(FrameSourceRequest request)
    {
        var filter = BuildFilter(request);
        var builder = new StringBuilder(256);

        builder.Append("-v error -hide_banner -nostdin ");

        if (request.StartAt > TimeSpan.Zero)
        {
            builder.Append("-ss ");
            builder.Append(FormatSeconds(request.StartAt));
            builder.Append(' ');
        }

        builder.Append("-i \"");
        builder.Append(Escape(request.InputPath));
        builder.Append("\" -an -sn -dn ");

        if (request.Duration is not null)
        {
            builder.Append("-t ");
            builder.Append(FormatSeconds(request.Duration.Value));
            builder.Append(' ');
        }

        if (request.Threads is int threads)
        {
            builder.Append("-threads ");
            builder.Append(threads);
            builder.Append(' ');
        }

        builder.Append("-pix_fmt rgb24 -f rawvideo ");

        if (!string.IsNullOrWhiteSpace(filter))
        {
            builder.Append("-vf \"");
            builder.Append(filter);
            builder.Append("\" ");
        }

        builder.Append("pipe:1");
        return builder.ToString();
    }

    private static string BuildFilter(FrameSourceRequest request)
    {
        var filters = new List<string>(4);

        if (request.CropWidth > 0 &&
            request.CropHeight > 0 &&
            (request.CropX != 0 ||
             request.CropY != 0 ||
             request.CropWidth != request.OutputWidth ||
             request.CropHeight != request.OutputHeight))
        {
            filters.Add(FormattableString.Invariant(
                $"crop={request.CropWidth}:{request.CropHeight}:{request.CropX}:{request.CropY}"));
        }

        if (request.Brightness != 0d ||
            Math.Abs(request.Contrast - 1d) > 0.001d ||
            Math.Abs(request.Saturation - 1d) > 0.001d ||
            Math.Abs(request.Gamma - 1d) > 0.001d)
        {
            filters.Add(
                $"eq=brightness={request.Brightness.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                $"contrast={request.Contrast.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                $"saturation={request.Saturation.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                $"gamma={request.Gamma.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        filters.Add(FormattableString.Invariant(
            $"scale={request.OutputWidth}:{request.OutputHeight}:flags={request.ScaleAlgorithm}"));

        if (request.LimitFrameRate)
        {
            filters.Add(FormattableString.Invariant(
                $"fps={request.OutputFrameRate.ToString("0.###", CultureInfo.InvariantCulture)}"));
        }

        return string.Join(',', filters);
    }

    private static string FormatSeconds(TimeSpan value) =>
        value.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
