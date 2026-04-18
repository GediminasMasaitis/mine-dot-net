using System.Collections.Generic;
using MineDotNet.AI.Solvers;

namespace MineDotNet.GUI.Models
{
    // User-editable entry in the benchmark's solver list. Each entry becomes
    // one "column" in the results — one outcome tally per generated board.
    public sealed class BenchmarkSolverConfig
    {
        public BenchmarkSolverConfig(string name, BorderSeparationSolverSettings settings)
        {
            Name = name;
            Settings = settings;
        }
        public string Name { get; set; }
        public BorderSeparationSolverSettings Settings { get; set; }
    }

    public enum BenchmarkOutcome
    {
        Won,
        Lost,   // Solver opened a mine.
        Stuck   // Solver returned zero results before revealing all safe cells.
    }

    // One solver's result on one generated board. Kept per-game so we can later
    // plot distributions (time, iterations) rather than just mean/variance.
    public sealed class BenchmarkGameResult
    {
        public int GameIndex { get; set; }
        public BenchmarkOutcome Outcome { get; set; }
        public int Iterations { get; set; }
        public double ElapsedMs { get; set; }
        public int CellsOpened { get; set; }
    }

    // Rolling stats for a single solver across all games completed so far.
    // UI reads this directly for the running counters; the finished benchmark
    // hands off the same object for final render + (later) graph plotting.
    public sealed class BenchmarkSolverRun
    {
        public BenchmarkSolverRun(int index, string name)
        {
            Index = index;
            Name = name;
            Games = new List<BenchmarkGameResult>();
        }

        public int Index { get; }
        public string Name { get; }
        public List<BenchmarkGameResult> Games { get; }

        public int GamesPlayed => Games.Count;
        public int Won { get; set; }
        public int Lost { get; set; }
        public int Stuck { get; set; }
        public double TotalMs { get; set; }
        public int TotalIterations { get; set; }

        public double WinRate => GamesPlayed == 0 ? 0 : (double)Won / GamesPlayed;
        public double AvgMs => GamesPlayed == 0 ? 0 : TotalMs / GamesPlayed;
        public double AvgIterations => GamesPlayed == 0 ? 0 : (double)TotalIterations / GamesPlayed;
    }

    // Full benchmark configuration — everything BenchmarkRunner needs to execute
    // and what BenchmarkDialog round-trips with the user.
    public sealed class BenchmarkConfig
    {
        public int Width { get; set; } = 30;
        public int Height { get; set; } = 16;
        public double MineDensity { get; set; } = 0.20;
        public int GameCount { get; set; } = 100;
        public List<BenchmarkSolverConfig> Solvers { get; set; } = new List<BenchmarkSolverConfig>();
    }
}
