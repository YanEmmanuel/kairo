using Kairo.Core.Contracts;
using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;
using Kairo.Rendering.Pipeline;

namespace Kairo.Rendering.Modes;

public sealed class BrailleRenderMode : IRenderMode
{
    private static readonly int[,] DotMap =
    {
        { 0x01, 0x08 },
        { 0x02, 0x10 },
        { 0x04, 0x20 },
        { 0x40, 0x80 }
    };

    public RenderModeKind Kind => RenderModeKind.Braille;

    public ModeDescriptor Descriptor => ModeCatalog.GetDescriptor(Kind);

    public void Render(VideoFrame source, FrameLayout layout, TerminalFrameBuffer destination, ResolvedPlaybackSettings settings)
    {
        destination.Clear();
        Span<Rgb24> samples = stackalloc Rgb24[8];
        var useBayer = settings.DitherMode == DitherMode.Bayer;

        for (var y = 0; y < layout.ContentHeight; y++)
        {
            for (var x = 0; x < layout.ContentWidth; x++)
            {
                var bits = 0;
                var sampleCount = 0;

                for (var dotY = 0; dotY < 4; dotY++)
                {
                    for (var dotX = 0; dotX < 2; dotX++)
                    {
                        var sourceX = (x * 2) + dotX;
                        var sourceY = (y * 4) + dotY;
                        var color = FramePixelReader.Read(source, sourceX, sourceY);
                        samples[sampleCount++] = color;
                        var luminance = useBayer
                            ? BayerDither.Apply(color.Luminance, sourceX, sourceY, 8)
                            : color.Luminance;

                        if (luminance >= 0.42d)
                        {
                            bits |= DotMap[dotY, dotX];
                        }
                    }
                }

                var glyph = bits == 0 ? ' ' : (char)(0x2800 + bits);
                var foreground = settings.ColorEnabled ? Rgb24.Average(samples[..sampleCount]) : Rgb24.White;
                destination.GetCellRef(layout.ContentX + x, layout.ContentY + y) = new TerminalCell(glyph, foreground, Rgb24.Black);
            }
        }
    }
}
