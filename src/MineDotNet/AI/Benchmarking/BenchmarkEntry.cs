using System;
using System.Collections.Generic;
using MineDotNet.Game;

namespace MineDotNet.AI.Benchmarking
{
    public class BenchmarkEntry
    {
        public BenchmarkEntry()
        {
            SolvingDuarations = new List<TimeSpan>();
        }

        public GameMap GameMap { get; set; }
        public int MineCount { get; set; }
        public IList<TimeSpan> SolvingDuarations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool Solved { get; set; }
    }
}