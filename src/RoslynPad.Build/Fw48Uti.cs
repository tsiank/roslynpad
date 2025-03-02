using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Buffers;

namespace RoslynPad.Build
{
    public static class HexConverter
    {
        public static string ToHexString(Span<byte> bytes, int length)
        {
            if (length > bytes.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            char[] hexChars = new char[length * 2];
            for (int i = 0; i < length; i++)
            {
                byte b = bytes[i];
                hexChars[i * 2] = ToHexDigit(b >> 4);
                hexChars[i * 2 + 1] = ToHexDigit(b & 0xF);
            }
            return new string(hexChars);
        }

        private static char ToHexDigit(int value)
        {
            return (char)(value < 10 ? '0' + value : 'A' + (value - 10));
        }
    }

    public static class FileHelper
    {
        public static IAsyncEnumerable<string> ReadLinesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            return ReadLinesAsyncImpl(path, cancellationToken);
        }

        private static async IAsyncEnumerable<string> ReadLinesAsyncImpl(string path, [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            using (var reader = new StreamReader(path))
            {
                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return line;
                }
            }
        }

        public static Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            return Task.Run(() =>
            {
                var lines = new List<string>();
                using (var reader = new StreamReader(path))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            throw new TaskCanceledException();
                        lines.Add(line);
                    }
                }
                return lines.ToArray();
            }, cancellationToken);
        }

        public static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            return Task.Run(async () =>
            {
                using (var reader = new StreamReader(path))
                {
                    // 检查取消请求
                    cancellationToken.ThrowIfCancellationRequested();

                    // 异步读取全部内容
                    string content = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // 再次检查取消请求（确保在返回前未被取消）
                    cancellationToken.ThrowIfCancellationRequested();

                    return content;
                }
            }, cancellationToken);
        }

        public static Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            return Task.Run(async () =>
            {
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                using (var writer = new StreamWriter(path, append: false))
                {
                    await writer.WriteAsync(contents).ConfigureAwait(false);

                    // 再次检查取消请求（确保在返回前未被取消）
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }, cancellationToken);
        }

        public static void Move(string sourceFileName, string destFileName, bool overwrite)
        {
            if (string.IsNullOrEmpty(sourceFileName))
                throw new ArgumentNullException(nameof(sourceFileName));
            if (string.IsNullOrEmpty(destFileName))
                throw new ArgumentNullException(nameof(destFileName));

            if (overwrite && File.Exists(destFileName))
            {
                File.Delete(destFileName);
            }
            File.Move(sourceFileName, destFileName);
        }
    }
    public static class RegexExtensions
    {
        public static ReadOnlySpan<char> ValueSpan(this Group group)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            // 获取 Group 的 Value 并转换为 ReadOnlySpan<char>
            return group.Value.AsSpan();
        }
    }

    public class IncrementalHash : IDisposable
    {
        private readonly HashAlgorithm _hashAlgorithm;
        private bool _isDisposed;

        public static IncrementalHash CreateHash(HashAlgorithmName algorithmName)
        {
            return new IncrementalHash(algorithmName);
        }

        private IncrementalHash(HashAlgorithmName algorithmName)
        {
            _hashAlgorithm = algorithmName.Name switch
            {
                "SHA256" => SHA256.Create(),
                "SHA1" => SHA1.Create(),
                "MD5" => MD5.Create(),
                _ => throw new ArgumentException("不支持的哈希算法", nameof(algorithmName))
            };
            _isDisposed = false;
        }

        public void AppendData(byte[] data)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(IncrementalHash));
            _hashAlgorithm.TransformBlock(data, 0, data.Length, null, 0);
        }

        // 新增重载支持 ReadOnlySpan<byte>
        public unsafe void AppendData(ReadOnlySpan<byte> data)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(IncrementalHash));

            fixed (byte* ptr = data)
            {
                //_hashAlgorithm.TransformBlock(
                //    null, // 直接传递空数组，后面用不安全指针覆盖
                //    0,
                //    data.Length,
                //    null,
                //    0
                //);
                // 注意：此处需要底层支持直接操作指针，实际仍需 byte[]
                // .NET Framework 不支持此方式，需回退到 ToArray
                //byte[] buffer = data.ToArray(); // 临时方案
                //_hashAlgorithm.TransformBlock(buffer, 0, buffer.Length, null, 0);
            }

            byte[] buffer = data.ToArray(); // 临时方案
            _hashAlgorithm.TransformBlock(buffer, 0, buffer.Length, null, 0);
        }
        public bool TryGetHashAndReset(Span<byte> hashBuffer, out int bytesWritten)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(IncrementalHash));

            bytesWritten = 0;
            _hashAlgorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            byte[] hash = _hashAlgorithm.Hash;

            if (hashBuffer.Length < hash.Length)
                return false;

            hash.CopyTo(hashBuffer);
            bytesWritten = hash.Length;
            _hashAlgorithm.Initialize();
            return true;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _hashAlgorithm.Dispose();
                _isDisposed = true;
            }
        }
    }

    // 模拟 HashAlgorithmName（.NET Framework 中不存在）
    public readonly struct HashAlgorithmName
    {
        public string Name { get; }

        public HashAlgorithmName(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public static HashAlgorithmName SHA256 => new HashAlgorithmName("SHA256");
        public static HashAlgorithmName SHA1 => new HashAlgorithmName("SHA1");
        public static HashAlgorithmName MD5 => new HashAlgorithmName("MD5");
    }


    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class MemberNotNullWhenAttribute : Attribute
    {
        public bool ReturnValue { get; }
        public string[] Members { get; }

        public MemberNotNullWhenAttribute(bool returnValue, params string[] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }
    }

    public static class StreamExtensions
    {
        public static ValueTask<int> ReadAsync(this Stream stream, Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (buffer.Length == 0)
                return new ValueTask<int>(0);

            // 如果 Memory<byte> 是基于数组的，直接使用底层数组
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> array))
            {
                return new ValueTask<int>(stream.ReadAsync(array.Array, array.Offset, array.Count, cancellationToken));
            }

            // 否则，从 ArrayPool 租用缓冲区
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            return FinishReadAsync(stream.ReadAsync(sharedBuffer, 0, buffer.Length, cancellationToken), sharedBuffer, buffer);

            static async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
            {
                try
                {
                    int result = await readTask.ConfigureAwait(false);
                    if (result > 0)
                    {
                        new ReadOnlySpan<byte>(localBuffer, 0, result).CopyTo(localDestination.Span);
                    }
                    return result;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(localBuffer);
                }
            }
        }
    }

    public static class Int32Extensions
    {
        public static int Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (s.IsEmpty)
                throw new FormatException("输入为空");

            provider ??= CultureInfo.CurrentCulture;
            NumberFormatInfo nfi = NumberFormatInfo.GetInstance(provider);

            s = s.Trim();
            bool isNegative = false;
            int index = 0;

            // 处理符号
            if (s.Length > 0)
            {
                if (s.StartsWith(nfi.NegativeSign.AsSpan()))
                {
                    isNegative = true;
                    index += nfi.NegativeSign.Length;
                }
                else if (s.StartsWith(nfi.PositiveSign.AsSpan()))
                {
                    index += nfi.PositiveSign.Length;
                }
            }

            long result = 0;
            bool hasDigits = false;

            // 解析数字和千分位分隔符
            for (; index < s.Length; index++)
            {
                char c = s[index];
                if (char.IsDigit(c))
                {
                    int digit = c - '0';
                    result = result * 10 + digit;
                    hasDigits = true;

                    if (result > int.MaxValue)
                        throw new OverflowException("值超出 Int32 范围");
                }
                else if (c == nfi.NumberGroupSeparator[0] && hasDigits)
                {
                    // 忽略千分位分隔符，但要求前面有数字
                    continue;
                }
                else
                {
                    throw new FormatException($"无效字符: '{c}'");
                }
            }

            if (!hasDigits)
                throw new FormatException("没有有效的数字");

            int finalResult = (int)(isNegative ? -result : result);
            if (isNegative && finalResult > 0 || !isNegative && finalResult < 0)
                throw new OverflowException("值超出 Int32 范围");

            return finalResult;
        }
    }

    public static class ConvertHelper
    {
        private static readonly char[] HexDigits = "0123456789abcdef".ToCharArray();

        public static string ToHexStringLower(byte[] bt)
        {
            if (bt == null)
                throw new ArgumentNullException(nameof(bt));

            if (bt.Length == 0)
                return string.Empty;

            char[] hexChars = new char[bt.Length * 2];
            for (int i = 0; i < bt.Length; i++)
            {
                byte b = bt[i];
                hexChars[i * 2] = HexDigits[b >> 4];     // 高 4 位
                hexChars[i * 2 + 1] = HexDigits[b & 0xF]; // 低 4 位
            }
            return new string(hexChars);
        }
    }

    public class CustomStream : Stream
    {
        private readonly Stream _baseStream;

        public CustomStream(Stream baseStream)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;

        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position { get => _baseStream.Position; set => _baseStream.Position = value; }

        public override void Flush() => _baseStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);
        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
        }

        public virtual void Write(ReadOnlyMemory<byte> buffer)
        {
            if (_baseStream == null)
                throw new ObjectDisposedException(nameof(CustomStream));

            if (buffer.Length == 0)
                return;

            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
            {
                _baseStream.Write(segment.Array, segment.Offset, segment.Count);
            }
            else
            {
                byte[] array = buffer.ToArray();
                _baseStream.Write(array, 0, array.Length);
            }
        }

        // 提供 Span 的重载
        public virtual void Write(ReadOnlySpan<byte> buffer)
        {
            Write(buffer.ToArray().AsMemory()); // 转换为 Memory 并调用
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public virtual ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_baseStream == null)
                throw new ObjectDisposedException(nameof(CustomStream));

            // 同步完成的情况
            if (buffer.Length == 0)
                return default;

            // 检查是否可以直接访问底层数组
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
            {
                return new ValueTask(_baseStream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
            }

            // 回退到 ToArray
            byte[] array = buffer.ToArray();
            return new ValueTask(_baseStream.WriteAsync(array, 0, array.Length, cancellationToken));
        }
    }

}
