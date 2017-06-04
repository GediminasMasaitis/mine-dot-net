using System.Collections.Generic;

namespace MineDotNet.AI.Benchmarking
{
    public class BenchmarkDensityGroup
    {
        public BenchmarkDensityGroup(IEnumerable<BenchmarkEntry> entries, int mineCount, double density)
        {
            MineCount = mineCount;
            Density = density;
            Entries = entries;
        }
        public int MineCount { get; }
        public double Density { get; }
        public IEnumerable<BenchmarkEntry> Entries { get; }
    }
}