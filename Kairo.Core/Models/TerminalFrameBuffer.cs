using System.Buffers;

namespace Kairo.Core.Models;

public sealed class TerminalFrameBuffer : IDisposable
{
    private readonly ArrayPool<TerminalCell> _pool;
    private TerminalCell[]? _cells;

    public TerminalFrameBuffer(int width, int height, ArrayPool<TerminalCell>? pool = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        Length = width * height;
        _pool = pool ?? ArrayPool<TerminalCell>.Shared;
        _cells = _pool.Rent(Length);
        Clear();
    }

    public int Width { get; }

    public int Height { get; }

    public int Length { get; }

    public Span<TerminalCell> Cells =>
        (_cells ?? throw new ObjectDisposedException(nameof(TerminalFrameBuffer))).AsSpan(0, Length);

    public ref TerminalCell GetCellRef(int x, int y)
    {
        if ((uint)x >= (uint)Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if ((uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        return ref (_cells ?? throw new ObjectDisposedException(nameof(TerminalFrameBuffer)))[(y * Width) + x];
    }

    public void Clear()
    {
        Cells.Fill(TerminalCell.Empty);
    }

    public void Dispose()
    {
        var cells = Interlocked.Exchange(ref _cells, null);
        if (cells is not null)
        {
            _pool.Return(cells, clearArray: false);
        }
    }
}
