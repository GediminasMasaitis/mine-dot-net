using System;
using System.Collections.Generic;
using System.Linq;

namespace MineDotNet.AI
{
    internal static class MultiCartesianExtension
    {
        public static IEnumerable<TInput[]> MultiCartesian<TInput>(this IEnumerable<IEnumerable<TInput>> input)
        {
            return input.MultiCartesian(x => x);
        }

        public static IEnumerable<TOutput> MultiCartesian<TInput, TOutput>(this IEnumerable<IEnumerable<TInput>> input, Func<TInput[], TOutput> selector)
        {
            // Materializing here to avoid multiple enumerations.
            var inputList = input as IList<IEnumerable<TInput>> ?? input.ToList();
            var buffer = new TInput[inputList.Count];
            var results = MultiCartesianInner(inputList, buffer, 0);
            var transformed = results.Select(selector);
            return transformed;
        }

        private static IEnumerable<TInput[]> MultiCartesianInner<TInput>(IList<IEnumerable<TInput>> input, TInput[] buffer, int depth)
        {
            foreach (var current in input[depth])
            {
                buffer[depth] = current;
                if (depth == buffer.Length - 1)
                {
                    // This is to ensure usage safety - the original buffer
                    // needs to remain unmodified to ensure a correct sequence.
                    var bufferCopy = (TInput[])buffer.Clone();
                    yield return bufferCopy;
                }
                else
                {
                    // Funky recursion here
                    var multiCartesianInner = MultiCartesianInner(input, buffer, depth + 1);
                    foreach (var a in multiCartesianInner)
                    {
                        yield return a;
                    }
                }
            }
        }
    }
}