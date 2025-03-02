using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RoslynPad.UI;
    public class SearchValues<T> where T : IEquatable<T>
    {
        private readonly T[] _values;

        public SearchValues(T[] values)
        {
            if (values == null || values.Length == 0)
            {
                throw new ArgumentException("Values cannot be null or empty.", nameof(values));
            }
            _values = values;
        }

        public static SearchValues<T> Create(params T[] values)
        {
            return new SearchValues<T>(values);
        }

        public virtual int IndexOfAny(ReadOnlySpan<T> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (_values.Contains(span[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public virtual int IndexOfAnyExcept(ReadOnlySpan<T> span)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (!_values.Contains(span[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public class SearchValuesByte : SearchValues<byte>
    {
        private readonly byte[] _values;
        private readonly BitVector256 _bitVector;

        public SearchValuesByte(byte[] values) : base(values)
        {
            _values = values;
            _bitVector = CreateBitVector(values);
        }

        private static BitVector256 CreateBitVector(byte[] values)
        {
            var vector = new BitVector256();
            foreach (byte value in values)
            {
                vector.Set(value);
            }
            return vector;
        }

        public override int IndexOfAny(ReadOnlySpan<byte> span)
        {
            ref byte spanRef = ref MemoryMarshal.GetReference(span);
            for (int i = 0; i < span.Length; i++)
            {
                if (_bitVector.Contains(spanRef))
                {
                    return i;
                }
                spanRef = ref Unsafe.Add(ref spanRef, 1);
            }
            return -1;
        }

        public override int IndexOfAnyExcept(ReadOnlySpan<byte> span)
        {
            ref byte spanRef = ref MemoryMarshal.GetReference(span);
            for (int i = 0; i < span.Length; i++)
            {
                if (!_bitVector.Contains(spanRef))
                {
                    return i;
                }
                spanRef = ref Unsafe.Add(ref spanRef, 1);
            }
            return -1;
        }
    }

    internal readonly struct BitVector256
    {
        private readonly uint[] _values = new uint[8];

        public BitVector256()
        {
        }

        public void Set(byte value)
        {
            uint index = (uint)value >> 5;
            uint mask = 1u << (value & 0x1F);
            _values[index] |= mask;
        }

        public bool Contains(byte value)
        {
            uint index = (uint)value >> 5;
            uint mask = 1u << (value & 0x1F);
            return (_values[index] & mask) != 0;
        }

        public byte[] GetValues()
        {
            var result = new List<byte>();
            for (int i = 0; i < 256; i++)
            {
                if (Contains((byte)i))
                {
                    result.Add((byte)i);
                }
            }
            return result.ToArray();
        }
    }

public static class ReadOnlySpanExtensions
{
    public static int IndexOfAny(this ReadOnlySpan<char> span, SearchValues<char> values)
    {
        return values.IndexOfAny(span);
    }

    public static int IndexOfAnyExcept(this ReadOnlySpan<char> span, SearchValues<char> values)
    {
        return values.IndexOfAnyExcept(span);
    }
}


public static class StringExtensions
{
    /// <summary>
    /// 扩展方法：检查字符串是否包含指定子串（支持比较规则）。
    /// </summary>
    /// <param name="source">原始字符串</param>
    /// <param name="value">要查找的子串</param>
    /// <param name="comparisonType">字符串比较规则</param>
    /// <returns>若包含则返回 true，否则返回 false</returns>
    public static bool Contains(
        this string source,
        string value,
        StringComparison comparisonType
    )
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return source.IndexOf(value, comparisonType) >= 0;
    }
}
