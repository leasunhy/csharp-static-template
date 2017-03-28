using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StaticTemplate
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Zips <paramref name="enum1"/> with <paramref name="enum2"/> and return the tuples.
        /// </summary>
        /// <typeparam name="T">The element type of <paramref name="enum1"/>.</typeparam>
        /// <typeparam name="F">The element type of <paramref name="enum2"/>.</typeparam>
        /// <param name="enum1">The first sequence.</param>
        /// <param name="enum2">The second sequence.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Zips two sequences and calls an action on each resulting tuple.
        /// </summary>
        /// <typeparam name="T">The element type of <paramref name="enum1"/>.</typeparam>
        /// <typeparam name="F">The element type of <paramref name="enum2"/>.</typeparam>
        /// <param name="enum1">The first sequence.</param>
        /// <param name="enum2">The second sequence.</param>
        /// <param name="action">The action to be called on the tuples.</param>
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

        /// <summary>
        /// Select the max element(s) in the sequence, keys computed using <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <param name="enumeration">The sequence.</param>
        /// <param name="keySelector">A function that takes an element and returns its key.</param>
        /// <returns>The tuple (maxKey, items), with items being the max elements,
        /// and maxKey being the key of the max elements.</returns>
        public static Tuple<TKey, IEnumerable<T>> MaxBy<T, TKey>(this IEnumerable<T> enumeration,
                                                                 Func<T, TKey> keySelector)
            where TKey : IComparable<TKey>
        {
            var enumerable = enumeration as IList<T> ?? enumeration.ToList();
            var keys = enumerable.Select(keySelector).ToList();
            var maxKey = keys.Max();
            var results = new List<T>();
            enumerable.ZipWith(keys, (t, k) => { if (k.CompareTo(maxKey) == 0) results.Add(t); });
            return Tuple.Create(maxKey, (IEnumerable<T>)results);
        }

        /// <summary>
        /// Select the min element(s) in the sequence, keys computed using <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <param name="enumeration">The sequence.</param>
        /// <param name="keySelector">A function that takes an element and returns its key.</param>
        /// <returns>The tuple (minKey, items), with items being the min elements,
        /// and minKey being the key of the min elements.</returns>
        public static Tuple<TKey, IEnumerable<T>> MinBy<T, TKey>(this IEnumerable<T> enumeration,
                                                                 Func<T, TKey> keySelector)
            where TKey : IComparable<TKey>
        {
            var enumerable = enumeration as IList<T> ?? enumeration.ToList();
            var keys = enumerable.Select(keySelector).ToList();
            var minKey = keys.Min();
            var results = new List<T>();
            enumerable.ZipWith(keys, (t, k) => { if (k.CompareTo(minKey) == 0) results.Add(t); });
            return Tuple.Create(minKey, (IEnumerable<T>)results);
        }
    }
}
