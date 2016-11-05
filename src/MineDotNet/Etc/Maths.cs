using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MineDotNet.Etc
{
    public static class Maths
    {
        private static IDictionary<int, IDictionary<int, decimal>> CombinationRatios { get; set; }

        static Maths()
        {
            InitializeCombinationRatios();
        }

        private static void InitializeCombinationRatios()
        {
            CombinationRatios = new Dictionary<int, IDictionary<int, decimal>>();
            for (var n = 0; n < 1000; n++)
            {
                var ratios = new Dictionary<int, decimal>();
                CombinationRatios[n] = ratios;
                ratios[0] = 1;
                for (var k = 1; k <= n; k++)
                {
                    var ratio = (n + 1 - k) / (decimal)k;
                    ratios[k] = ratio;
                }
            }
        }

        public static decimal CombinationRatio(int from, int count)
        {
            if (count > from)
            {
                throw new ArgumentException();
            }
            return CombinationRatios[from][count];
        }
    }
}
