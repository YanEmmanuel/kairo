using System.Diagnostics;
using Kairo.Core.Contracts;
using Kairo.Core.Playback;

namespace Kairo.Video.FFmpeg;

public sealed class YtDlpMediaInputResolver : IMediaInputResolver
{
    private readonly string _downloadDirectory;

    public YtDlpMediaInputResolver(string? downloadDirectory = null)
    {
        _downloadDirectory = downloadDirectory ?? GetDefaultDownloadDirectory();
    }

    public async Task<ResolvedMediaInput> ResolveAsync(string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new InvalidOperationException("An input path or URL is required.");
        }

        if (LooksLikeRemoteInput(input))
        {
            return await DownloadRemoteAsync(input, cancellationToken).ConfigureAwait(false);
        }

        var localPath = TryResolveFileUri(input, out var fileUriPath)
            ? fileUriPath
            : Path.GetFullPath(input);

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Input file '{localPath}' was not found.", localPath);
        }

        return new ResolvedMediaInput(input, localPath, false, false);
    }

    public static bool LooksLikeRemoteInput(string input) =>
        Uri.TryCreate(input, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    internal static string? TryResolveDownloadedPath(string output, string downloadDirectory)
    {
        foreach (var line in output
                     .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Reverse())
        {
            var candidate = Path.IsPathRooted(line)
                ? line
                : Path.GetFullPath(line, downloadDirectory);

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<ResolvedMediaInput> DownloadRemoteAsync(string input, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_downloadDirectory);

        Process process;

        try
        {
            var ffmpegLocation = ExternalToolLocator.ResolveOptional("ffmpeg") is { } ffmpegPath
                ? Path.GetDirectoryName(ffmpegPath)
                : null;
            var denoPath = ExternalToolLocator.ResolveOptional("deno");
            var startInfo = ExternalProcessStartInfoFactory.Create(
                "yt-dlp",
                YtDlpArgumentBuilder.BuildDownload(input, _downloadDirectory, ffmpegLocation, denoPath),
                redirectStandardOutput: true,
                redirectStandardError: true);

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start yt-dlp.");
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException(
                "URL playback requires the bundled 'yt-dlp' binary or a system 'yt-dlp' in PATH. Use the portable release bundle or download the file locally first.",
                exception);
        }

        using (process)
        {
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

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "yt-dlp exited with an error." : stderr.Trim());
            }

            var resolvedPath = TryResolveDownloadedPath(stdout, _downloadDirectory);
            if (resolvedPath is null)
            {
                throw new InvalidOperationException("yt-dlp finished without returning a playable file path.");
            }

            return new ResolvedMediaInput(input, resolvedPath, true, true);
        }
    }

    private static bool TryResolveFileUri(string input, out string localPath)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            localPath = uri.LocalPath;
            return true;
        }

        localPath = string.Empty;
        return false;
    }

    private static string GetDefaultDownloadDirectory()
    {
        var cacheRoot = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (!string.IsNullOrWhiteSpace(cacheRoot))
        {
            return Path.Combine(cacheRoot, "kairo", "downloads");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
        {
            return Path.Combine(home, ".cache", "kairo", "downloads");
        }

        return Path.Combine(Path.GetTempPath(), "kairo-downloads");
    }
}
