using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RoslynPad.Roslyn
{
    public static class ArgumentNullExceptionE
    {
        public static void ThrowIfNull(object argument)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(nameof(argument));
            }
        }
    }


    public unsafe static class MetadataHelper
    {
        public static bool TryGetRawMetadata(Assembly assembly, out byte* metadataPtr, out int length)
        {
            metadataPtr = null;
            length = 0;

            // 获取程序集文件路径（仅适用于从文件加载的程序集）
            string assemblyPath = assembly.Location;
            if (string.IsNullOrEmpty(assemblyPath)) return false;

            try
            {
                // 读取程序集文件流
                using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
                using var peReader = new PEReader(stream);

                // 获取元数据块信息
                var metadataBlock = peReader.GetMetadata();
                byte[] metadataBytes = new byte[metadataBlock.Length];
                metadataBlock.GetContent().CopyTo(metadataBytes);

                // 固定元数据内存并返回指针
                GCHandle handle = GCHandle.Alloc(metadataBytes, GCHandleType.Pinned);
                metadataPtr = (byte*)handle.AddrOfPinnedObject().ToPointer();
                length = metadataBytes.Length;

                // 注意：此处需要调用方负责释放 GCHandle！
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 释放内存的方法（由调用方显式调用）
        public static void FreeMetadata(byte* metadataPtr, GCHandle handle)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    public static class EnumerableExtensions
    {
        // 实现 ToHashSet()
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        // 可选：支持自定义比较器
        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> source,
            IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata.
    /// This class should not be used by developers in source code.
    /// This dummy class is required to compile records when targeting .NET Standard
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
