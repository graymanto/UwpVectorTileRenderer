using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace VectorTilesRenderer.Extensions
{
    public static class CollectionExtensions
    {
        [DebuggerStepThrough]
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> func)
        {
            foreach (var item in collection ?? Enumerable.Empty<T>())
            {
                func(item);
            }
        }

        [DebuggerStepThrough]
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> input, int batchSize)
        {
            if (input == null)
                input = Enumerable.Empty<T>();

            var temp = new List<T>(batchSize);
            foreach (var item in input)
            {
                temp.Add(item);

                if (temp.Count == batchSize)
                {
                    yield return temp;
                    temp = new List<T>(batchSize);
                }
            }
            if (temp.Count > 0)
                yield return temp;
        }
    }
}
