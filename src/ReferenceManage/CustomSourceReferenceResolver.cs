using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace ReferenceManage;

public class CustomSourceReferenceResolver : SourceReferenceResolver
{
    private readonly string _rootPath;
    private readonly IList<string> _searchPaths;

    public CustomSourceReferenceResolver(string rootPath, IList<string>? searchPaths = null)
    {
        _rootPath = rootPath;
        _searchPaths = searchPaths ?? new List<string>();
    }

    public override bool Equals(object? other) => throw new NotImplementedException();
    public override int GetHashCode() => throw new NotImplementedException();

    public override string NormalizePath(string path, string? baseFilePath)
    {
        // 如果是绝对路径，直接返回
        if (Path.IsPathRooted(path))
            return path;

        // 先尝试相对于基础文件路径查找
        if (!string.IsNullOrEmpty(baseFilePath))
        {
            var basePath = Path.GetDirectoryName(baseFilePath);
            var combinedPath = Path.Combine(basePath, path);
            if (File.Exists(combinedPath))
                return combinedPath;
        }

        // 尝试相对于根路径查找
        var rootCombinedPath = Path.Combine(_rootPath, path);
        if (File.Exists(rootCombinedPath))
            return rootCombinedPath;

        // 在搜索路径中查找
        foreach (var searchPath in _searchPaths)
        {
            var searchCombinedPath = Path.Combine(searchPath, path);
            if (File.Exists(searchCombinedPath))
                return searchCombinedPath;
        }

        // 如果都找不到，返回原始路径
        return path;
    }

    public override Stream OpenRead(string resolvedPath)
    {
        // 打开文件流
        return File.OpenRead(resolvedPath);
    }

    public override string ResolveReference(string path, string? baseFilePath)
    {
        return NormalizePath(path, baseFilePath);
    }
}
