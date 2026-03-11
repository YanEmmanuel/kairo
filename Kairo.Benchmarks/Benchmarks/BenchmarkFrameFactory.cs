using System.Buffers;
using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;

namespace Kairo.Benchmarks.Benchmarks;

internal static class BenchmarkFrameFactory
{
    public static VideoFrame CreateFrame(int width, int height)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(width * height * 3);
        var span = buffer.AsSpan(0, width * height * 3);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var offset = ((y * width) + x) * 3;
                span[offset] = (byte)((x * 255) / Math.Max(1, width - 1));
                span[offset + 1] = (byte)((y * 255) / Math.Max(1, height - 1));
                span[offset + 2] = (byte)(((x + y) * 255) / Math.Max(1, width + height - 2));
            }
        }

        return new VideoFrame(width, height, 0, TimeSpan.Zero, buffer, width * height * 3, ArrayPool<byte>.Shared);
    }

    public static ResolvedPlaybackSettings CreateSettings(RenderModeKind mode, int terminalWidth, int terminalHeight) =>
        new(
            mode,
            ModeCatalog.GetDescriptor(mode),
            ResizeMode.Fit,
            true,
            true,
            false,
            false,
            24d,
            3,
            0,
            false,
            null,
            " .,:;irsXA253hMHGS#9B&@",
            false,
            DitherMode.Bayer,
            new TerminalSize(terminalWidth, terminalHeight),
            new FrameLayout(
                terminalWidth,
                terminalHeight,
                0,
                0,
                terminalWidth,
                terminalHeight,
                ModeCatalog.GetDescriptor(mode).HorizontalDensity,
                ModeCatalog.GetDescriptor(mode).VerticalDensity,
                0,
                0,
                terminalWidth * ModeCatalog.GetDescriptor(mode).HorizontalDensity,
                terminalHeight * ModeCatalog.GetDescriptor(mode).VerticalDensity),
            "bilinear",
            TimeSpan.Zero,
            null,
            false,
            false,
            false,
            false,
            false,
            false);
}
