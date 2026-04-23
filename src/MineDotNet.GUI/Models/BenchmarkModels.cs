using System;
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
    // hands off the same object for final render + graph plotting.
    //
    // In sweep mode, the runner produces one instance per (solver, axis value)
    // pair — SolverIndex stays shared across one solver's series, AxisValue
    // provides the x-axis coordinate. Outside sweep mode AxisValue is null.
    public sealed class BenchmarkSolverRun
    {
        public BenchmarkSolverRun(int solverIndex, string name, double? axisValue = null)
        {
            SolverIndex = solverIndex;
            Name = name;
            AxisValue = axisValue;
            Games = new List<BenchmarkGameResult>();
        }

        public int SolverIndex { get; }
        public string Name { get; }
        public double? AxisValue { get; }
        public List<BenchmarkGameResult> Games { get; }

        // Back-compat alias for older call sites that indexed by "Index".
        public int Index => SolverIndex;

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

    public enum BenchmarkSweepAxis
    {
        None,
        Width,
        Height,
        MineDensity,
        // Sweeps one numeric property of BorderSeparationSolverSettings across
        // SweepFrom..SweepTo. Board dims stay fixed; each solver in the list
        // plays the same boards with the picked property overridden per value.
        SolverParameter
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

        // Sweep settings. When SweepAxis != None, Width/Height/MineDensity on
        // the axis is ignored and replaced with values in [SweepFrom, SweepTo]
        // stepping by SweepStep. The runner produces one BenchmarkSolverRun per
        // (solver, axis value) pair.
        public BenchmarkSweepAxis SweepAxis { get; set; } = BenchmarkSweepAxis.None;
        public double SweepFrom { get; set; }
        public double SweepTo { get; set; }
        public double SweepStep { get; set; } = 1;

        // Name of the BorderSeparationSolverSettings property being swept when
        // SweepAxis == SolverParameter. Ignored for all other axes.
        public string SweepParameterName { get; set; }

        // Expand the configured sweep into the concrete list of axis values the
        // runner should iterate over. Clamps step to a sane minimum so a zero /
        // negative step doesn't turn the benchmark into an infinite loop.
        public IReadOnlyList<double> SweepValues()
        {
            if (SweepAxis == BenchmarkSweepAxis.None) return Array.Empty<double>();
            var step = Math.Max(SweepStep, SweepAxis == BenchmarkSweepAxis.MineDensity ? 0.001 : 1);
            var from = Math.Min(SweepFrom, SweepTo);
            var to = Math.Max(SweepFrom, SweepTo);
            var values = new List<double>();
            for (var v = from; v <= to + 1e-9; v += step) values.Add(v);
            return values;
        }
    }
}
