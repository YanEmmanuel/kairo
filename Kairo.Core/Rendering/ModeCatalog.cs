namespace Kairo.Core.Rendering;

public static class ModeCatalog
{
    public static ModeDescriptor GetDescriptor(RenderModeKind kind) =>
        kind switch
        {
            RenderModeKind.Ascii => new ModeDescriptor(kind, 1, 1),
            RenderModeKind.Blocks => new ModeDescriptor(kind, 1, 2),
            RenderModeKind.Braille => new ModeDescriptor(kind, 2, 4),
            RenderModeKind.Emoji => new ModeDescriptor(kind, 1, 1),
            _ => new ModeDescriptor(RenderModeKind.Blocks, 1, 2)
        };
}
