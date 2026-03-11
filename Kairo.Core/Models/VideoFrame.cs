using System.Buffers;

namespace Kairo.Core.Models;

public sealed class VideoFrame : IDisposable
{
    private readonly ArrayPool<byte> _pool;
    private byte[]? _buffer;

    public VideoFrame(
        int width,
        int height,
        long frameIndex,
        TimeSpan timestamp,
        byte[] buffer,
        int bufferLength,
        ArrayPool<byte>? pool = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        Width = width;
        Height = height;
        FrameIndex = frameIndex;
        Timestamp = timestamp;
        BufferLength = bufferLength;
        _buffer = buffer;
        _pool = pool ?? ArrayPool<byte>.Shared;
    }

    public int Width { get; }

    public int Height { get; }

    public long FrameIndex { get; }

    public TimeSpan Timestamp { get; }

    public int BufferLength { get; }

    public ReadOnlySpan<byte> Pixels =>
        (_buffer ?? throw new ObjectDisposedException(nameof(VideoFrame))).AsSpan(0, BufferLength);

    public Span<byte> WritablePixels =>
        (_buffer ?? throw new ObjectDisposedException(nameof(VideoFrame))).AsSpan(0, BufferLength);

    public void Dispose()
    {
        var buffer = Interlocked.Exchange(ref _buffer, null);
        if (buffer is not null)
        {
            _pool.Return(buffer, clearArray: false);
        }
    }
}
