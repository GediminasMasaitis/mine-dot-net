using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MineDotNet.Etc
{
    static class LinqExtensions
    {
        public static IEnumerable<int> IndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var index = 0;
            foreach (var element in source)
            {
                if (predicate.Invoke(element))
                {
                    yield return index;
                }
                index++;
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var index = 0;
            foreach (var element in source)
            {
                if (predicate.Invoke(element))
                {
                    return index;
                }
                index++;
            }
            throw new InvalidOperationException();
        }
    }
}
