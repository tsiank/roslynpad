using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace RoslynPad.Themes;

public static class SpanParser
{
    public static int Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
    {
        if (s.Length == 0)
            throw new FormatException("Input span is empty.");

        // 获取格式信息
        NumberFormatInfo formatInfo = provider == null
            ? NumberFormatInfo.InvariantInfo
            : NumberFormatInfo.GetInstance(provider);

        // 处理符号和进制
        bool isNegative = false;
        int startIndex = 0;
        int radix = 10;

        // 处理十六进制
        if ((style & NumberStyles.AllowHexSpecifier) != 0)
        {
            radix = 16;
            style &= ~NumberStyles.AllowHexSpecifier;
        }

        // 处理符号
        if ((style & NumberStyles.AllowLeadingSign) != 0)
        {
            string negativeSign = formatInfo.NegativeSign;
            string positiveSign = formatInfo.PositiveSign;
          
            if (s.StartsWith(negativeSign))
            {
                isNegative = true;
                startIndex += negativeSign.Length;
            }
            else if (s.StartsWith(positiveSign))
            {
                startIndex += positiveSign.Length;
            }
        }

        // 解析数字
        int result = 0;
        for (int i = startIndex; i < s.Length; i++)
        {
            char c = s[i];
            int digit = 0;

            // 十进制解析
            if (radix == 10)
            {
                if (c < '0' || c > '9')
                    ThrowFormatException(c, i);
                digit = c - '0';
            }
            // 十六进制解析
            else
            {
                if (c >= '0' && c <= '9')
                    digit = c - '0';
                else if (c >= 'A' && c <= 'F')
                    digit = c - 'A' + 10;
                else if (c >= 'a' && c <= 'f')
                    digit = c - 'a' + 10;
                else
                    ThrowFormatException(c, i);
            }

            checked { result = result * radix + digit; }
        }

        return isNegative ? -result : result;
    }

    // 辅助方法：检查是否以指定字符串开头
    private static bool StartsWith(this ReadOnlySpan<char> span, string value)
    {
        if (value.Length > span.Length) return false;
        for (int i = 0; i < value.Length; i++)
        {
            if (span[i] != value[i]) return false;
        }
        return true;
    }

    private static void ThrowFormatException(char c, int position) =>
    throw new FormatException($"Invalid character '{c}' at position {position}.");
}

public static class StackExtensions
{
    public static bool TryPop<T>(this Stack<T> stack, [MaybeNullWhen(false)] out T result)
    {
        if (stack == null)
            throw new ArgumentNullException(nameof(stack));

        if (stack.Count > 0)
        {
            result = stack.Pop();
            return true;
        }

        result = default(T);
        return false;
    }
}
