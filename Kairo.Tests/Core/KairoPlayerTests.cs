using System.Buffers;
using System.Threading.Channels;
using Kairo.Core.Contracts;
using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;

namespace Kairo.Tests.Core;

public sealed class KairoPlayerTests
{
    [Fact]
    public async Task PlayAsync_PrefersSmoothPlaybackForInsaneDetail()
    {
        var snapshot = await PlayAsync(DetailLevel.Insane, outputDelay: TimeSpan.FromMilliseconds(40));

        Assert.Equal(6, snapshot.DecodedFrames);
        Assert.Equal(6, snapshot.RenderedFrames);
        Assert.Equal(0, snapshot.DroppedFrames);
    }

    [Fact]
    public async Task PlayAsync_DropsFramesForBalancedDetailWhenRendererFallsBehind()
    {
        var snapshot = await PlayAsync(DetailLevel.Balanced, outputDelay: TimeSpan.FromMilliseconds(40));

        Assert.Equal(6, snapshot.DecodedFrames);
        Assert.True(snapshot.RenderedFrames < snapshot.DecodedFrames);
        Assert.True(snapshot.DroppedFrames > 0);
    }

    private static async Task<PlaybackStatsSnapshot> PlayAsync(DetailLevel detail, TimeSpan outputDelay)
    {
        var player = new KairoPlayer(
            new StubVideoProbe(),
            new StubFrameSource(frameCount: 6),
            new StubTerminalDevice(outputDelay),
            new Dictionary<RenderModeKind, IRenderMode>
            {
                [RenderModeKind.Ascii] = new StubRenderMode(RenderModeKind.Ascii)
            });

        return await player.PlayAsync(
            new PlaybackOptions
            {
                InputPath = "video.mp4",
                Mode = RenderModeKind.Ascii,
                Detail = detail,
                Width = 1,
                Height = 1
            },
            CancellationToken.None);
    }

    private sealed class StubVideoProbe : IVideoProbe
    {
        public Task<VideoMetadata> ProbeAsync(string inputPath, CancellationToken cancellationToken) =>
            Task.FromResult(new VideoMetadata(1, 1, 60d, TimeSpan.FromSeconds(1), 6, "rgb24"));
    }

    private sealed class StubFrameSource(int frameCount) : IFrameSource
    {
        public async Task ProduceAsync(FrameSourceRequest request, ChannelWriter<VideoFrame> writer, CancellationToken cancellationToken)
        {
            var frameSize = request.OutputWidth * request.OutputHeight * 3;
            var frameDuration = TimeSpan.FromSeconds(1d / request.OutputFrameRate);

            try
            {
                for (var index = 0; index < frameCount; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var buffer = ArrayPool<byte>.Shared.Rent(frameSize);
                    Array.Clear(buffer, 0, frameSize);
                    request.Stats.OnFrameDecoded(frameSize);

                    await writer.WriteAsync(
                        new VideoFrame(
                            request.OutputWidth,
                            request.OutputHeight,
                            index,
                            TimeSpan.FromTicks(frameDuration.Ticks * index),
                            buffer,
                            frameSize,
                            ArrayPool<byte>.Shared),
                        cancellationToken);
                }

                writer.TryComplete();
            }
            catch (Exception exception)
            {
                writer.TryComplete(exception);
                throw;
            }
        }
    }

    private sealed class StubTerminalDevice(TimeSpan outputDelay) : ITerminalDevice
    {
        public TerminalSize GetCurrentSize() => new(80, 24);

        public ValueTask EnterAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask ExitAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public async ValueTask RenderAsync(
            TerminalFrameBuffer current,
            TerminalFrameBuffer previous,
            bool forceFullRedraw,
            CancellationToken cancellationToken)
        {
            await Task.Delay(outputDelay, cancellationToken);
        }
    }

    private sealed class StubRenderMode(RenderModeKind kind) : IRenderMode
    {
        public RenderModeKind Kind => kind;
        public ModeDescriptor Descriptor => ModeCatalog.GetDescriptor(kind);

        public void Render(VideoFrame source, FrameLayout layout, TerminalFrameBuffer destination, ResolvedPlaybackSettings settings)
        {
            destination.Cells[0] = new TerminalCell('@', Rgb24.White, Rgb24.Black);
        }
    }
}
