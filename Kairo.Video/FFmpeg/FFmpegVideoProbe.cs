using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Kairo.Core.Contracts;
using Kairo.Core.Models;

namespace Kairo.Video.FFmpeg;

public sealed class FFmpegVideoProbe : IVideoProbe
{
    public async Task<VideoMetadata> ProbeAsync(string inputPath, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffprobe",
            Arguments = FFmpegArgumentBuilder.BuildProbe(inputPath),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start ffprobe.");
        using var registration = cancellationToken.Register(static state =>
        {
            if (state is Process running && !running.HasExited)
            {
                try
                {
                    running.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            }
        }, process);

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = await errorTask.ConfigureAwait(false);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "ffprobe exited with an error." : stderr.Trim());
        }

        var output = await outputTask.ConfigureAwait(false);
        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;
        var streams = root.GetProperty("streams");

        foreach (var stream in streams.EnumerateArray())
        {
            if (!string.Equals(stream.GetProperty("codec_type").GetString(), "video", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var width = stream.GetProperty("width").GetInt32();
            var height = stream.GetProperty("height").GetInt32();
            var frameRate = ParseFraction(GetOptionalString(stream, "avg_frame_rate")) switch
            {
                > 0d and var fps => fps,
                _ => ParseFraction(GetOptionalString(stream, "r_frame_rate"))
            };

            var duration = ParseDuration(GetOptionalString(stream, "duration"));
            if (duration == TimeSpan.Zero && root.TryGetProperty("format", out var formatElement))
            {
                duration = ParseDuration(GetOptionalString(formatElement, "duration"));
            }

            var frameCount = ParseNullableLong(GetOptionalString(stream, "nb_frames"));
            var pixelFormat = GetOptionalString(stream, "pix_fmt") ?? "unknown";

            return new VideoMetadata(width, height, frameRate, duration, frameCount, pixelFormat);
        }

        throw new InvalidOperationException("No video stream was found in the provided input.");
    }

    private static string? GetOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) ? property.GetString() : null;

    private static double ParseFraction(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0d;
        }

        var parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 1 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var direct))
        {
            return direct;
        }

        if (parts.Length != 2 ||
            !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var numerator) ||
            !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var denominator) ||
            denominator == 0d)
        {
            return 0d;
        }

        return numerator / denominator;
    }

    private static TimeSpan ParseDuration(string? value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds <= 0d)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(seconds);
    }

    private static long? ParseNullableLong(string? value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : null;
}
