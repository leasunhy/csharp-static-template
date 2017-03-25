using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticTemplate
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<Tuple<T, F>> Zip<T, F>(this IEnumerable<T> enum1, IEnumerable<F> enum2)
        {
            if (enum1 == null || enum2 == null) { yield break; }
            using (var enumerator1 = enum1.GetEnumerator())
            using (var enumerator2 = enum2.GetEnumerator())
            {
                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    yield return Tuple.Create(enumerator1.Current, enumerator2.Current);
                }
            }
        }

        public static void ZipWith<T, F>(this IEnumerable<T> enum1, IEnumerable<F> enum2, Action<T, F> action)
        {
            if (enum1 == null || enum2 == null) { return; }
            using (var enumerator1 = enum1.GetEnumerator())
            using (var enumerator2 = enum2.GetEnumerator())
            {
                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    action(enumerator1.Current, enumerator2.Current);
                }
            }
        }

        public static Tuple<K, IEnumerable<T>> MaxBy<T, K>(this IEnumerable<T> enumeration,
                                                           Func<T, K> keySelector)
            where K : IComparable<K>
        {
            var keys = enumeration.Select(keySelector);
            var maxKey = keys.Max();
            var results = new List<T>();
            enumeration.ZipWith(keys, (t, k) => { if (k.CompareTo(maxKey) == 0) results.Add(t); });
            return Tuple.Create(maxKey, (IEnumerable<T>)results);
        }

        public static Tuple<K, IEnumerable<T>> MinBy<T, K>(this IEnumerable<T> enumeration,
                                                           Func<T, K> keySelector)
            where K : IComparable<K>
        {
            var keys = enumeration.Select(keySelector);
            var minKey = keys.Min();
            var results = new List<T>();
            enumeration.ZipWith(keys, (t, k) => { if (k.CompareTo(minKey) == 0) results.Add(t); });
            return Tuple.Create(minKey, (IEnumerable<T>)results);
        }
    }
}
