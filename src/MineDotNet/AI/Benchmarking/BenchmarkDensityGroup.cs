using System.Collections.Generic;

namespace MineDotNet.AI.Benchmarking
{
    public class BenchmarkDensityGroup
    {
        public BenchmarkDensityGroup(IEnumerable<BenchmarkEntry> entries, double density)
        {
            Density = density;
            Entries = entries;
        }

        public double Density { get; }
        public IEnumerable<BenchmarkEntry> Entries { get; }
    }
}