using System;
using System.Collections.Generic;
using MineDotNet.Game;
using MineDotNet.Game.Models;

namespace MineDotNet.AI.Benchmarking
{
    public class BenchmarkEntry
    {
        public BenchmarkEntry()
        {
            SolvingDuarations = new List<TimeSpan>();
        }
        public int Index { get; set; }
        public GameMap GameMap { get; set; }
        public int? MineCount { get; set; }
        public IList<TimeSpan> SolvingDuarations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool Solved { get; set; }
        public bool FailedOnFlagging { get; set; }
    }
}