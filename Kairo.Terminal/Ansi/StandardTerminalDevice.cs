using System.Text;
using Kairo.Core.Contracts;
using Kairo.Core.Models;

namespace Kairo.Terminal.Ansi;

public sealed class StandardTerminalDevice : ITerminalDevice, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly PooledAnsiBuilder _builder;
    private readonly Stream _stdout;
    private bool _entered;

    public StandardTerminalDevice()
    {
        Console.OutputEncoding = Encoding.UTF8;
        _stdout = Console.OpenStandardOutput();
        _writer = new StreamWriter(_stdout)
        {
            AutoFlush = false
        };
        _builder = new PooledAnsiBuilder();
    }

    public TerminalSize GetCurrentSize()
    {
        try
        {
            var width = Math.Max(1, Console.WindowWidth);
            var height = Math.Max(1, Console.WindowHeight);
            return new TerminalSize(width, height);
        }
        catch
        {
            return new TerminalSize(80, 24);
        }
    }

    public async ValueTask EnterAsync(CancellationToken cancellationToken)
    {
        if (_entered)
        {
            return;
        }

        _builder.Reset();
        _builder.Append("\u001b[?25l\u001b[2J\u001b[H");
        _writer.Write(_builder.WrittenSpan);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        _entered = true;
    }

    public async ValueTask ExitAsync(CancellationToken cancellationToken)
    {
        if (!_entered)
        {
            return;
        }

        _builder.Reset();
        _builder.Append("\u001b[0m\u001b[?25h");
        _writer.Write(_builder.WrittenSpan);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        _entered = false;
    }

    public async ValueTask RenderAsync(
        TerminalFrameBuffer current,
        TerminalFrameBuffer previous,
        bool forceFullRedraw,
        CancellationToken cancellationToken)
    {
        _builder.Reset();
        AnsiDiffRenderer.Render(current, previous, _builder, forceFullRedraw);
        _writer.Write(_builder.WrittenSpan);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _writer.Flush();
        _builder.Dispose();
    }
}
