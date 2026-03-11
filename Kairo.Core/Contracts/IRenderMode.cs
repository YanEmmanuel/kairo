using Kairo.Core.Models;
using Kairo.Core.Playback;
using Kairo.Core.Rendering;

namespace Kairo.Core.Contracts;

public interface IRenderMode
{
    RenderModeKind Kind { get; }

    ModeDescriptor Descriptor { get; }

    void Render(VideoFrame source, FrameLayout layout, TerminalFrameBuffer destination, ResolvedPlaybackSettings settings);
}
