using System.Buffers;
using System.Globalization;
using System.Text;
using Kairo.Core.Models;

namespace Kairo.Terminal.Ansi;

public sealed class PooledAnsiBuilder : IDisposable
{
    private char[]? _buffer;
    private readonly ArrayPool<char> _pool;
    private int _length;

    public PooledAnsiBuilder(int initialCapacity = 4_096, ArrayPool<char>? pool = null)
    {
        _pool = pool ?? ArrayPool<char>.Shared;
        _buffer = _pool.Rent(initialCapacity);
    }

    public ReadOnlySpan<char> WrittenSpan =>
        (_buffer ?? throw new ObjectDisposedException(nameof(PooledAnsiBuilder))).AsSpan(0, _length);

    public void Reset()
    {
        _length = 0;
    }

    public void Append(char value)
    {
        EnsureCapacity(1);
        _buffer![_length++] = value;
    }

    public void Append(string value)
    {
        EnsureCapacity(value.Length);
        value.AsSpan().CopyTo(_buffer!.AsSpan(_length));
        _length += value.Length;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        EnsureCapacity(value.Length);
        value.CopyTo(_buffer!.AsSpan(_length));
        _length += value.Length;
    }

    public void AppendCursorPosition(int row, int column)
    {
        Append("\u001b[");
        AppendInt(row);
        Append(';');
        AppendInt(column);
        Append('H');
    }

    public void AppendForeground(Rgb24 color)
    {
        Append("\u001b[38;2;");
        AppendInt(color.R);
        Append(';');
        AppendInt(color.G);
        Append(';');
        AppendInt(color.B);
        Append('m');
    }

    public void AppendBackground(Rgb24 color)
    {
        Append("\u001b[48;2;");
        AppendInt(color.R);
        Append(';');
        AppendInt(color.G);
        Append(';');
        AppendInt(color.B);
        Append('m');
    }

    public void AppendReset() => Append("\u001b[0m");

    public void Dispose()
    {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null)
        {
            _pool.Return(buffer, clearArray: false);
        }
    }

    private void AppendInt(int value)
    {
        Span<char> digits = stackalloc char[16];
        if (!value.TryFormat(digits, out var written, provider: CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Unable to format ANSI integer.");
        }

        Append(digits[..written]);
    }

    private void EnsureCapacity(int additionalLength)
    {
        ObjectDisposedException.ThrowIf(_buffer is null, this);

        var required = _length + additionalLength;
        if (required <= _buffer.Length)
        {
            return;
        }

        var next = _pool.Rent(Math.Max(required, _buffer.Length * 2));
        _buffer.AsSpan(0, _length).CopyTo(next);
        _pool.Return(_buffer, clearArray: false);
        _buffer = next;
    }
}
