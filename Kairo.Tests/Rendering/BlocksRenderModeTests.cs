using System.Buffers;
using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;
using Kairo.Rendering.Modes;

namespace Kairo.Tests.Rendering;

public sealed class BlocksRenderModeTests
{
    [Fact]
    public void Render_UsesQuadrantGlyphsWhenLayoutHasHighDensity()
    {
        using var frame = CreateFrame(
            2,
            2,
            new Rgb24(255, 255, 255),
            Rgb24.Black,
            Rgb24.Black,
            Rgb24.Black);
        using var buffer = new TerminalFrameBuffer(1, 1);

        var layout = new FrameLayout(1, 1, 0, 0, 1, 1, 2, 2, 0, 0, 2, 2);
        var settings = CreateSettings(colorEnabled: false, layout);

        new BlocksRenderMode().Render(frame, layout, buffer, settings);

        var cell = buffer.Cells[0];
        Assert.Equal('▘', cell.Glyph);
        Assert.Equal(Rgb24.White, cell.Foreground);
        Assert.Equal(Rgb24.Black, cell.Background);
    }

    [Fact]
    public void Render_KeepsHalfBlockPathForDefaultDensity()
    {
        using var frame = CreateFrame(
            1,
            2,
            new Rgb24(255, 255, 255),
            Rgb24.Black);
        using var buffer = new TerminalFrameBuffer(1, 1);

        var layout = new FrameLayout(1, 1, 0, 0, 1, 1, 1, 2, 0, 0, 1, 2);
        var settings = CreateSettings(colorEnabled: false, layout);

        new BlocksRenderMode().Render(frame, layout, buffer, settings);

        Assert.Equal('▀', buffer.Cells[0].Glyph);
    }

    private static VideoFrame CreateFrame(int width, int height, params Rgb24[] pixels)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(width * height * 3);
        var span = buffer.AsSpan(0, width * height * 3);

        for (var index = 0; index < pixels.Length; index++)
        {
            var offset = index * 3;
            span[offset] = pixels[index].R;
            span[offset + 1] = pixels[index].G;
            span[offset + 2] = pixels[index].B;
        }

        return new VideoFrame(width, height, 0, TimeSpan.Zero, buffer, width * height * 3, ArrayPool<byte>.Shared);
    }

    private static ResolvedPlaybackSettings CreateSettings(bool colorEnabled, FrameLayout layout) =>
        new(
            RenderModeKind.Blocks,
            new ModeDescriptor(RenderModeKind.Blocks, layout.HorizontalDensity, layout.VerticalDensity),
            ResizeMode.Fit,
            colorEnabled,
            true,
            false,
            false,
            24d,
            3,
            null,
            " .,:;irsXA253hMHGS#9B&@",
            false,
            DitherMode.None,
            new TerminalSize(layout.TerminalWidth, layout.TerminalHeight),
            layout,
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
