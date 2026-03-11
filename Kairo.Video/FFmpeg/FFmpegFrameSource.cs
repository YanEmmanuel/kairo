using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Channels;
using Kairo.Core.Contracts;
using Kairo.Core.Models;
using Kairo.Core.Playback;

namespace Kairo.Video.FFmpeg;

public sealed class FFmpegFrameSource : IFrameSource
{
    private readonly ArrayPool<byte> _pool;

    public FFmpegFrameSource(ArrayPool<byte>? pool = null)
    {
        _pool = pool ?? ArrayPool<byte>.Shared;
    }

    public async Task ProduceAsync(FrameSourceRequest request, ChannelWriter<VideoFrame> writer, CancellationToken cancellationToken)
    {
        Process? process = null;
        Task<string>? stderrTask = null;

        try
        {
            var startInfo = ExternalProcessStartInfoFactory.Create(
                "ffmpeg",
                FFmpegArgumentBuilder.BuildDecode(request),
                redirectStandardOutput: true,
                redirectStandardError: true);

            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start ffmpeg.");
            stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

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

            var stream = process.StandardOutput.BaseStream;
            var frameSize = checked(request.OutputWidth * request.OutputHeight * 3);
            var frameIndex = 0L;
            var frameDuration = request.OutputFrameRate <= 0d
                ? TimeSpan.FromSeconds(1d / 24d)
                : TimeSpan.FromSeconds(1d / request.OutputFrameRate);

            while (!cancellationToken.IsCancellationRequested)
            {
                var buffer = _pool.Rent(frameSize);
                var bytesRead = await ReadFullFrameAsync(stream, buffer, frameSize, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    _pool.Return(buffer, clearArray: false);
                    break;
                }

                if (bytesRead < frameSize)
                {
                    _pool.Return(buffer, clearArray: false);
                    break;
                }

                request.Stats.OnFrameDecoded(bytesRead);

                var frame = new VideoFrame(
                    request.OutputWidth,
                    request.OutputHeight,
                    frameIndex,
                    TimeSpan.FromTicks(frameDuration.Ticks * frameIndex),
                    buffer,
                    frameSize,
                    _pool);

                await writer.WriteAsync(frame, cancellationToken).ConfigureAwait(false);
                frameIndex++;
            }

            writer.TryComplete();
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                var stderr = stderrTask is null ? string.Empty : await stderrTask.ConfigureAwait(false);
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "ffmpeg exited with an error." : stderr.Trim());
            }
        }
        catch (OperationCanceledException)
        {
            writer.TryComplete();
            throw;
        }
        catch (Exception exception)
        {
            writer.TryComplete(exception);
            throw;
        }
        finally
        {
            process?.Dispose();
        }
    }

    private static async ValueTask<int> ReadFullFrameAsync(Stream stream, byte[] buffer, int frameSize, CancellationToken cancellationToken)
    {
        var totalRead = 0;

        while (totalRead < frameSize)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead, frameSize - totalRead), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        return totalRead;
    }
}
