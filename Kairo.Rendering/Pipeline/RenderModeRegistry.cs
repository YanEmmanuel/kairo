using Kairo.Core.Contracts;
using Kairo.Core.Rendering;
using Kairo.Rendering.Modes;

namespace Kairo.Rendering.Pipeline;

public static class RenderModeRegistry
{
    public static IReadOnlyDictionary<RenderModeKind, IRenderMode> CreateDefault() =>
        new Dictionary<RenderModeKind, IRenderMode>
        {
            [RenderModeKind.Ascii] = new AsciiRenderMode(),
            [RenderModeKind.Blocks] = new BlocksRenderMode(),
            [RenderModeKind.Braille] = new BrailleRenderMode()
        };
}
