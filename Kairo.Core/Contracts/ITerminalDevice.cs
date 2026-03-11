using Kairo.Core.Models;

namespace Kairo.Core.Contracts;

public interface ITerminalDevice
{
    TerminalSize GetCurrentSize();

    ValueTask EnterAsync(CancellationToken cancellationToken);

    ValueTask ExitAsync(CancellationToken cancellationToken);

    ValueTask RenderAsync(TerminalFrameBuffer current, TerminalFrameBuffer previous, bool forceFullRedraw, CancellationToken cancellationToken);
}
