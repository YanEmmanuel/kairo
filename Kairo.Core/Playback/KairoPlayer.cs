using System.Diagnostics;
using System.Threading.Channels;
using Kairo.Core.Contracts;
using Kairo.Core.Models;
using Kairo.Core.Rendering;

namespace Kairo.Core.Playback;

public sealed class KairoPlayer
{
    private readonly IVideoProbe _videoProbe;
    private readonly IFrameSource _frameSource;
    private readonly ITerminalDevice _terminalDevice;
    private readonly IReadOnlyDictionary<RenderModeKind, IRenderMode> _renderModes;
    private readonly IAudioPlayer? _audioPlayer;

    public KairoPlayer(
        IVideoProbe videoProbe,
        IFrameSource frameSource,
        ITerminalDevice terminalDevice,
        IReadOnlyDictionary<RenderModeKind, IRenderMode> renderModes,
        IAudioPlayer? audioPlayer = null)
    {
        _videoProbe = videoProbe;
        _frameSource = frameSource;
        _terminalDevice = terminalDevice;
        _renderModes = renderModes;
        _audioPlayer = audioPlayer;
    }

    public async Task<PlaybackStatsSnapshot> PlayAsync(PlaybackOptions options, CancellationToken cancellationToken)
    {
        Validate(options);

        var metadata = await _videoProbe.ProbeAsync(options.PlaybackPath, cancellationToken).ConfigureAwait(false);
        var stats = new PlaybackStats();
        var segmentStart = options.StartAt;
        var remainingDuration = options.Duration;

        await _terminalDevice.EnterAsync(cancellationToken).ConfigureAwait(false);
        IAsyncDisposable? audioSession = null;
        Func<CancellationToken, Task>? ensureAudioStartedAsync = null;

        try
        {
            if (options.Audio == AudioMode.On)
            {
                if (_audioPlayer is null)
                {
                    throw new InvalidOperationException("Audio playback is not available in this build.");
                }

                if (!options.PreviewFrame)
                {
                    ensureAudioStartedAsync = async token =>
                    {
                        if (audioSession is not null)
                        {
                            return;
                        }

                        audioSession = await _audioPlayer
                            .StartAsync(new AudioPlaybackRequest(options.PlaybackPath, options.StartAt, options.Duration, options.Loop), token)
                            .ConfigureAwait(false);
                    };
                }
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                var terminalSize = _terminalDevice.GetCurrentSize();
                var settings = PlaybackPlanner.Resolve(options, metadata, terminalSize);

                if (!_renderModes.TryGetValue(settings.Mode, out var renderMode))
                {
                    throw new NotSupportedException($"The render mode '{settings.Mode}' is not available in this build.");
                }

                var request = new FrameSourceRequest(
                    options.PlaybackPath,
                    segmentStart,
                    remainingDuration,
                    settings.Layout.ScaledWidth,
                    settings.Layout.ScaledHeight,
                    settings.Layout.SourceX,
                    settings.Layout.SourceY,
                    settings.Layout.SourceWidth,
                    settings.Layout.SourceHeight,
                    settings.TargetFps,
                    options.Fps is > 0d,
                    settings.Threads,
                    settings.ScaleAlgorithm,
                    options.Brightness,
                    options.Contrast,
                    options.Saturation,
                    options.Gamma,
                    stats);

                var startupBufferFrames = ShouldUseStartupBuffer(options, audioSession, segmentStart)
                    ? settings.StartupBufferFrames
                    : 0;
                var outcome = await PlaySegmentAsync(
                        settings,
                        request,
                        renderMode,
                        stats,
                        startupBufferFrames,
                        ensureAudioStartedAsync,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (audioSession is not null)
                {
                    ensureAudioStartedAsync = null;
                }

                if (options.PreviewFrame)
                {
                    break;
                }

                if (outcome.RestartRequested)
                {
                    segmentStart += outcome.LastTimestamp;

                    if (remainingDuration is not null)
                    {
                        var nextDuration = remainingDuration.Value - outcome.LastTimestamp;
                        if (nextDuration <= TimeSpan.Zero)
                        {
                            break;
                        }

                        remainingDuration = nextDuration;
                    }

                    continue;
                }

                if (options.Loop)
                {
                    segmentStart = options.StartAt;
                    remainingDuration = options.Duration;
                    continue;
                }

                break;
            }
        }
        finally
        {
            if (audioSession is not null)
            {
                await audioSession.DisposeAsync().ConfigureAwait(false);
            }

            await _terminalDevice.ExitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        return stats.Snapshot();
    }

    private async Task<SegmentOutcome> PlaySegmentAsync(
        ResolvedPlaybackSettings settings,
        FrameSourceRequest request,
        IRenderMode renderMode,
        PlaybackStats stats,
        int startupBufferFrames,
        Func<CancellationToken, Task>? onPlaybackStartAsync,
        CancellationToken cancellationToken)
    {
        using var current = new TerminalFrameBuffer(settings.Layout.TerminalWidth, settings.Layout.TerminalHeight);
        using var previous = new TerminalFrameBuffer(settings.Layout.TerminalWidth, settings.Layout.TerminalHeight);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var channel = Channel.CreateBounded<VideoFrame>(new BoundedChannelOptions(settings.BufferSize)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        var producerTask = _frameSource.ProduceAsync(request, channel.Writer, linkedCts.Token);
        var stopwatch = new Stopwatch();
        var sawFirstFrame = false;
        var lastTimestamp = TimeSpan.Zero;
        var restartRequested = false;
        var scheduleOffset = TimeSpan.Zero;
        var frameQueue = new Queue<VideoFrame>(Math.Max(1, settings.BufferSize));
        var frameBudget = settings.TargetFps > 0d
            ? TimeSpan.FromSeconds(1d / settings.TargetFps)
            : TimeSpan.FromMilliseconds(33);

        try
        {
            var hasFrames = await FillFrameQueueAsync(
                    channel.Reader,
                    frameQueue,
                    Math.Max(1, startupBufferFrames),
                    linkedCts.Token)
                .ConfigureAwait(false);

            while (hasFrames)
            {
                DrainAvailableFrames(channel.Reader, frameQueue, settings.BufferSize);
                var frame = frameQueue.Dequeue();

                using (frame)
                {
                    if (!sawFirstFrame)
                    {
                        if (onPlaybackStartAsync is not null)
                        {
                            await onPlaybackStartAsync(linkedCts.Token).ConfigureAwait(false);
                        }

                        stopwatch.Restart();
                        sawFirstFrame = true;
                    }

                    lastTimestamp = frame.Timestamp;
                    var skipRender = false;

                    if (!settings.MaxFps)
                    {
                        var delay = (frame.Timestamp + scheduleOffset) - stopwatch.Elapsed;
                        if (delay > TimeSpan.FromMilliseconds(1))
                        {
                            await Task.Delay(delay, linkedCts.Token).ConfigureAwait(false);
                        }
                        else if (-delay > frameBudget)
                        {
                            if (settings.PreferSmoothPlayback)
                            {
                                scheduleOffset += -delay;
                            }
                            else
                            {
                                stats.OnFrameDropped();
                                skipRender = true;
                            }
                        }
                    }

                    if (!skipRender)
                    {
                        var renderStarted = Stopwatch.GetTimestamp();
                        renderMode.Render(frame, settings.Layout, current, settings);
                        var renderDuration = Stopwatch.GetElapsedTime(renderStarted);

                        var outputStarted = Stopwatch.GetTimestamp();
                        await _terminalDevice
                            .RenderAsync(current, previous, !settings.UseDiffRendering || settings.ForceFullRedraw, linkedCts.Token)
                            .ConfigureAwait(false);
                        var outputDuration = Stopwatch.GetElapsedTime(outputStarted);

                        stats.OnFrameRendered(renderDuration, outputDuration);
                        SwapBuffers(current, previous);

                        if (settings.PreviewFrame)
                        {
                            linkedCts.Cancel();
                            break;
                        }

                        if (settings.TrackResize && HasTerminalResized(settings.TerminalSize))
                        {
                            restartRequested = true;
                            linkedCts.Cancel();
                            break;
                        }
                    }
                }

                DrainAvailableFrames(channel.Reader, frameQueue, settings.BufferSize);
                hasFrames = frameQueue.Count > 0 ||
                    await FillFrameQueueAsync(channel.Reader, frameQueue, 1, linkedCts.Token).ConfigureAwait(false);
            }

            await producerTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            await DrainFramesAsync(channel.Reader).ConfigureAwait(false);
        }
        finally
        {
            DisposeFrames(frameQueue);
        }

        return new SegmentOutcome(restartRequested, lastTimestamp);
    }

    private static bool ShouldUseStartupBuffer(PlaybackOptions options, IAsyncDisposable? audioSession, TimeSpan segmentStart) =>
        segmentStart == options.StartAt &&
        (options.Audio == AudioMode.Off || audioSession is null);

    private bool HasTerminalResized(TerminalSize initial) =>
        _terminalDevice.GetCurrentSize() is var current &&
        (current.Width != initial.Width || current.Height != initial.Height);

    private static void DrainAvailableFrames(ChannelReader<VideoFrame> reader, Queue<VideoFrame> queue, int maxFrames)
    {
        while (queue.Count < maxFrames && reader.TryRead(out var frame))
        {
            queue.Enqueue(frame);
        }
    }

    private static async Task<bool> FillFrameQueueAsync(
        ChannelReader<VideoFrame> reader,
        Queue<VideoFrame> queue,
        int targetCount,
        CancellationToken cancellationToken)
    {
        while (queue.Count < targetCount)
        {
            DrainAvailableFrames(reader, queue, targetCount);
            if (queue.Count >= targetCount)
            {
                return true;
            }

            if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return queue.Count > 0;
            }
        }

        return true;
    }

    private static async Task DrainFramesAsync(ChannelReader<VideoFrame> reader)
    {
        while (await reader.WaitToReadAsync().ConfigureAwait(false))
        {
            while (reader.TryRead(out var frame))
            {
                frame.Dispose();
            }
        }
    }

    private static void DisposeFrames(Queue<VideoFrame> queue)
    {
        while (queue.TryDequeue(out var frame))
        {
            frame.Dispose();
        }
    }

    private static void SwapBuffers(TerminalFrameBuffer current, TerminalFrameBuffer previous)
    {
        current.Cells.CopyTo(previous.Cells);
    }

    private static void Validate(PlaybackOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PlaybackPath))
        {
            throw new InvalidOperationException("An input path or URL is required.");
        }

        if (options.Mode == RenderModeKind.Emoji)
        {
            throw new NotSupportedException("Emoji mode is reserved for a future iteration of the renderer.");
        }

        if (options.HasUnsupportedExportRequest)
        {
            throw new NotSupportedException("Frame export and file export flags are reserved for a future iteration of Kairo.");
        }
    }

    private readonly record struct SegmentOutcome(bool RestartRequested, TimeSpan LastTimestamp);
}
