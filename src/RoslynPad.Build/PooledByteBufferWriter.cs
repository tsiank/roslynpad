﻿using System.Buffers;
using System.Diagnostics;

namespace RoslynPad.Build;

internal sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[] _rentedBuffer;
    private int _index;

    private const int MinimumBufferSize = 256;

    public PooledByteBufferWriter(int initialCapacity = MinimumBufferSize)
    {
        Debug.Assert(initialCapacity > 0);

        _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _index = 0;
    }

    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            Debug.Assert(_index <= _rentedBuffer!.Length);
            return _rentedBuffer.AsMemory(0, _index);
        }
    }

    public int WrittenCount
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            return _index;
        }
    }

    public int Capacity
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            return _rentedBuffer!.Length;
        }
    }

    public int FreeCapacity
    {
        get
        {
            Debug.Assert(_rentedBuffer != null);
            return _rentedBuffer!.Length - _index;
        }
    }

    public void Reset() => _index = 0;

    public void Clear() => ClearHelper();

    private void ClearHelper()
    {
        Debug.Assert(_rentedBuffer != null);
        Debug.Assert(_index <= _rentedBuffer!.Length);

        _rentedBuffer.AsSpan(0, _index).Clear();
        _index = 0;
    }

    // Returns the rented buffer back to the pool
    public void Dispose()
    {
        if (_rentedBuffer == null)
        {
            return;
        }

        ClearHelper();
        byte[] toReturn = _rentedBuffer;
        _rentedBuffer = null!;
        ArrayPool<byte>.Shared.Return(toReturn);
    }

    public void Advance(int count)
    {
        Debug.Assert(_rentedBuffer != null);
        Debug.Assert(count >= 0);
        Debug.Assert(_index <= _rentedBuffer!.Length - count);

        _index += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsMemory(_index);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        CheckAndResizeBuffer(sizeHint);
        return _rentedBuffer.AsSpan(_index);
    }

    internal ValueTask WriteToStreamAsync(CustomStream destination, CancellationToken cancellationToken) =>
        destination.WriteAsync(WrittenMemory, cancellationToken);

    internal void WriteToStream(CustomStream destination) => destination.Write(WrittenMemory.Span);

    private void CheckAndResizeBuffer(int sizeHint)
    {
        Debug.Assert(_rentedBuffer != null);
        Debug.Assert(sizeHint >= 0);

        if (sizeHint == 0)
        {
            sizeHint = MinimumBufferSize;
        }

        int availableSpace = _rentedBuffer!.Length - _index;

        if (sizeHint > availableSpace)
        {
            int currentLength = _rentedBuffer.Length;
            int growBy = Math.Max(sizeHint, currentLength);

            int newSize = currentLength + growBy;

            if ((uint)newSize > int.MaxValue)
            {
                newSize = currentLength + sizeHint;
                if ((uint)newSize > int.MaxValue)
                {
                    throw new InsufficientMemoryException("BufferMaximumSizeExceeded: " + (uint)newSize);
                }
            }

            byte[] oldBuffer = _rentedBuffer;

            _rentedBuffer = ArrayPool<byte>.Shared.Rent(newSize);

            Debug.Assert(oldBuffer.Length >= _index);
            Debug.Assert(_rentedBuffer.Length >= _index);

            Span<byte> previousBuffer = oldBuffer.AsSpan(0, _index);
            previousBuffer.CopyTo(_rentedBuffer);
            previousBuffer.Clear();
            ArrayPool<byte>.Shared.Return(oldBuffer);
        }

        Debug.Assert(_rentedBuffer.Length - _index > 0);
        Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
    }
}
