using Kairo.Core.Models;

namespace Kairo.Rendering.Pipeline;

public static class FramePixelReader
{
    public static Rgb24 Read(VideoFrame frame, int x, int y)
    {
        var pixels = frame.Pixels;
        var offset = ((y * frame.Width) + x) * 3;
        return new Rgb24(pixels[offset], pixels[offset + 1], pixels[offset + 2]);
    }
}
