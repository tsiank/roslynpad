using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynPad;

//public interface IReadOnlySet<T> : IReadOnlyCollection<T>
//{
//    bool Contains(T item);
//    bool IsSubsetOf(IEnumerable<T> other);
//    bool IsSupersetOf(IEnumerable<T> other);
//    bool Overlaps(IEnumerable<T> other);
//    bool SetEquals(IEnumerable<T> other);
//}

//public class CustomReadOnlySet<T> : IReadOnlySet<T>
//{
//    private readonly HashSet<T> _internalSet;

//    public CustomReadOnlySet(IEnumerable<T> items) => _internalSet = new HashSet<T>(items);

//    public int Count => _internalSet.Count;
//    public bool Contains(T item) => _internalSet.Contains(item);
//    public bool IsSubsetOf(IEnumerable<T> other) => _internalSet.IsSubsetOf(other);
//    public bool IsSupersetOf(IEnumerable<T> other) => _internalSet.IsSupersetOf(other);
//    public bool Overlaps(IEnumerable<T> other) => _internalSet.Overlaps(other);
//    public bool SetEquals(IEnumerable<T> other) => _internalSet.SetEquals(other);
//    public IEnumerator<T> GetEnumerator() => _internalSet.GetEnumerator();
//    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
//}

public static class MathExtensions
{
    public static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }
        else if (value > max)
        {
            return max;
        }
        else
        {
            return value;
        }
    }
}
