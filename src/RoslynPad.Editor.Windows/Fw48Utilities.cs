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

namespace RoslynPad.Editor
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

}
