using Kairo.Core.Contracts;
using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;
using Kairo.Rendering.Pipeline;

namespace Kairo.Rendering.Modes;

public sealed class AsciiRenderMode : IRenderMode
{
    public RenderModeKind Kind => RenderModeKind.Ascii;

    public ModeDescriptor Descriptor => ModeCatalog.GetDescriptor(Kind);

    public void Render(VideoFrame source, FrameLayout layout, TerminalFrameBuffer destination, ResolvedPlaybackSettings settings)
    {
        destination.Clear();

        var mapper = new LuminanceCharacterMapper(settings.Charset, settings.InvertCharset);
        var useBayer = settings.DitherMode == DitherMode.Bayer;

        for (var y = 0; y < layout.ContentHeight; y++)
        {
            for (var x = 0; x < layout.ContentWidth; x++)
            {
                var color = FramePixelReader.Read(source, x, y);
                var glyph = mapper.Map(color.Luminance, x, y, useBayer);
                var foreground = settings.ColorEnabled ? color : Rgb24.White;
                destination.GetCellRef(layout.ContentX + x, layout.ContentY + y) = new TerminalCell(glyph, foreground, Rgb24.Black);
            }
        }
    }
}
