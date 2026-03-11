using Kairo.Core.Contracts;
using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;
using Kairo.Rendering.Pipeline;

namespace Kairo.Rendering.Modes;

public sealed class BlocksRenderMode : IRenderMode
{
    private static readonly char[] QuadrantGlyphMap =
    [
        ' ',
        '▘',
        '▝',
        '▀',
        '▖',
        '▌',
        '▞',
        '▛',
        '▗',
        '▚',
        '▐',
        '▜',
        '▄',
        '▙',
        '▟',
        '█'
    ];

    public RenderModeKind Kind => RenderModeKind.Blocks;

    public ModeDescriptor Descriptor => ModeCatalog.GetDescriptor(Kind);

    public void Render(VideoFrame source, FrameLayout layout, TerminalFrameBuffer destination, ResolvedPlaybackSettings settings)
    {
        destination.Clear();

        if (layout.HorizontalDensity >= 2 && layout.VerticalDensity >= 2)
        {
            RenderQuadrants(source, layout, destination, settings);
            return;
        }

        RenderHalfBlocks(source, layout, destination, settings);
    }

    private static void RenderHalfBlocks(
        VideoFrame source,
        FrameLayout layout,
        TerminalFrameBuffer destination,
        ResolvedPlaybackSettings settings)
    {
        for (var y = 0; y < layout.ContentHeight; y++)
        {
            var topRow = y * 2;
            var bottomRow = Math.Min(topRow + 1, source.Height - 1);

            for (var x = 0; x < layout.ContentWidth; x++)
            {
                var top = FramePixelReader.Read(source, x, topRow);
                var bottom = FramePixelReader.Read(source, x, bottomRow);
                var cell = settings.ColorEnabled
                    ? new TerminalCell('▀', top, bottom)
                    : BuildMonochromeHalfBlockCell(top, bottom);

                destination.GetCellRef(layout.ContentX + x, layout.ContentY + y) = cell;
            }
        }
    }

    private static void RenderQuadrants(
        VideoFrame source,
        FrameLayout layout,
        TerminalFrameBuffer destination,
        ResolvedPlaybackSettings settings)
    {
        var useBayer = settings.DitherMode == DitherMode.Bayer;

        for (var y = 0; y < layout.ContentHeight; y++)
        {
            var sourceTop = y * 2;
            var sourceBottom = Math.Min(sourceTop + 1, source.Height - 1);

            for (var x = 0; x < layout.ContentWidth; x++)
            {
                var sourceLeft = x * 2;
                var sourceRight = Math.Min(sourceLeft + 1, source.Width - 1);

                var topLeft = FramePixelReader.Read(source, sourceLeft, sourceTop);
                var topRight = FramePixelReader.Read(source, sourceRight, sourceTop);
                var bottomLeft = FramePixelReader.Read(source, sourceLeft, sourceBottom);
                var bottomRight = FramePixelReader.Read(source, sourceRight, sourceBottom);

                var cell = settings.ColorEnabled
                    ? BuildColorQuadrantCell(topLeft, topRight, bottomLeft, bottomRight)
                    : BuildMonochromeQuadrantCell(
                        topLeft,
                        topRight,
                        bottomLeft,
                        bottomRight,
                        sourceLeft,
                        sourceTop,
                        sourceRight,
                        sourceBottom,
                        useBayer);

                destination.GetCellRef(layout.ContentX + x, layout.ContentY + y) = cell;
            }
        }
    }

    private static TerminalCell BuildMonochromeHalfBlockCell(Rgb24 top, Rgb24 bottom)
    {
        var topOn = top.Luminance >= 0.5d;
        var bottomOn = bottom.Luminance >= 0.5d;

        return (topOn, bottomOn) switch
        {
            (false, false) => TerminalCell.Empty,
            (true, false) => new TerminalCell('▀', Rgb24.White, Rgb24.Black),
            (false, true) => new TerminalCell('▄', Rgb24.White, Rgb24.Black),
            _ => new TerminalCell('█', Rgb24.White, Rgb24.Black)
        };
    }

    private static TerminalCell BuildMonochromeQuadrantCell(
        Rgb24 topLeft,
        Rgb24 topRight,
        Rgb24 bottomLeft,
        Rgb24 bottomRight,
        int sourceLeft,
        int sourceTop,
        int sourceRight,
        int sourceBottom,
        bool useBayer)
    {
        var mask = 0;

        if (ApplyThreshold(topLeft, sourceLeft, sourceTop, useBayer))
        {
            mask |= 0x01;
        }

        if (ApplyThreshold(topRight, sourceRight, sourceTop, useBayer))
        {
            mask |= 0x02;
        }

        if (ApplyThreshold(bottomLeft, sourceLeft, sourceBottom, useBayer))
        {
            mask |= 0x04;
        }

        if (ApplyThreshold(bottomRight, sourceRight, sourceBottom, useBayer))
        {
            mask |= 0x08;
        }

        return BuildQuadrantCell(mask, Rgb24.White, Rgb24.Black);
    }

    private static TerminalCell BuildColorQuadrantCell(Rgb24 topLeft, Rgb24 topRight, Rgb24 bottomLeft, Rgb24 bottomRight)
    {
        Span<Rgb24> samples =
        [
            topLeft,
            topRight,
            bottomLeft,
            bottomRight
        ];

        var bestMask = 0;
        var bestError = double.MaxValue;
        var bestForeground = Rgb24.Black;
        var bestBackground = Rgb24.Black;

        for (var mask = 0; mask < 16; mask++)
        {
            var foreground = AverageForMask(samples, mask, active: true);
            var background = AverageForMask(samples, mask, active: false);
            var error = ComputeMaskError(samples, mask, foreground, background);

            if (error < bestError)
            {
                bestError = error;
                bestMask = mask;
                bestForeground = foreground;
                bestBackground = background;
            }
        }

        return BuildQuadrantCell(bestMask, bestForeground, bestBackground);
    }

    private static TerminalCell BuildQuadrantCell(int mask, Rgb24 foreground, Rgb24 background) =>
        mask switch
        {
            0 => new TerminalCell(' ', background, background),
            15 => new TerminalCell('█', foreground, foreground),
            _ => new TerminalCell(QuadrantGlyphMap[mask], foreground, background)
        };

    private static bool ApplyThreshold(Rgb24 color, int x, int y, bool useBayer)
    {
        var luminance = useBayer
            ? BayerDither.Apply(color.Luminance, x, y, 8)
            : color.Luminance;

        return luminance >= 0.5d;
    }

    private static Rgb24 AverageForMask(ReadOnlySpan<Rgb24> samples, int mask, bool active)
    {
        Span<Rgb24> selected = stackalloc Rgb24[4];
        var count = 0;

        for (var index = 0; index < samples.Length; index++)
        {
            var isActive = (mask & (1 << index)) != 0;
            if (isActive != active)
            {
                continue;
            }

            selected[count++] = samples[index];
        }

        if (count == 0)
        {
            return Rgb24.Average(samples);
        }

        return Rgb24.Average(selected[..count]);
    }

    private static double ComputeMaskError(ReadOnlySpan<Rgb24> samples, int mask, Rgb24 foreground, Rgb24 background)
    {
        var error = 0d;

        for (var index = 0; index < samples.Length; index++)
        {
            var target = (mask & (1 << index)) != 0 ? foreground : background;
            error += ComputeColorError(samples[index], target);
        }

        return error;
    }

    private static double ComputeColorError(Rgb24 actual, Rgb24 expected)
    {
        var red = actual.R - expected.R;
        var green = actual.G - expected.G;
        var blue = actual.B - expected.B;
        return (red * red) + (green * green) + (blue * blue);
    }
}
