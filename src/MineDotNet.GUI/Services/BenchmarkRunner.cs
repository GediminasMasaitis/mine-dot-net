using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MineDotNet.AI.Guessers;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game;
using MineDotNet.Game.Models;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Services
{
    internal sealed class BenchmarkRunner
    {
        // Hard cap on solver iterations per game. A buggy solver that keeps
        // returning zero-verdict / zero-change results shouldn't be able to
        // hang the whole benchmark. 1000 is generous — real games terminate
        // in tens to low hundreds of iterations.
        private const int MaxIterationsPerGame = 1000;

        // Category-level timers, summed across the whole run. Surfaced so the
        // dialog can print a "where did the time go" block at the end.
        public double TotalSolverMs { get; private set; }
        public double TotalInitMs { get; private set; }
        public double TotalSnapshotMs { get; private set; }
        public double TotalEngineBuildMs { get; private set; }
        public double TotalEngineOpsMs { get; private set; }
        public double TotalGuesserMs { get; private set; }
        public int TotalSolveCalls { get; private set; }

        // Runs the configured benchmark end-to-end on the caller's thread.
        // Each game generates one board; every enabled solver plays the same
        // board so win-rate deltas reflect settings, not RNG luck. Any
        // parallelism comes from the solver's own settings — we don't layer
        // a Task.Run on top, which would conflict with ExtSolver's own threading.
        //
        // progress fires after each solver-on-board completes so the caller
        // can pump UI updates between regenerations.
        // When true, benchmark calls DirectSolver (P/Invoke into the shared
        // library) instead of ExtSolver (stdio to UMSI). Set via the dialog
        // checkbox before Run(). Defaults off — ExtSolver path is the proven
        // one, direct is experimental.
        public bool UseDirectSolver { get; set; }

        public IReadOnlyList<BenchmarkSolverRun> Run(
            BenchmarkConfig config,
            Action<BenchmarkProgressUpdate> onProgress = null,
            Func<bool> shouldStop = null)
        {
            // In sweep mode we produce SolverCount × AxisValues.Count runs and
            // iterate the outer loop over axis values, reusing each generated
            // board across solvers at that axis value. In non-sweep mode the
            // axis-value list degenerates to a single "virtual" entry so the
            // same control flow handles both cases.
            var axisValues = config.SweepAxis == BenchmarkSweepAxis.None
                ? new[] { double.NaN }
                : config.SweepValues().ToArray();

            var runs = new List<BenchmarkSolverRun>();
            foreach (var v in axisValues)
            {
                for (var i = 0; i < config.Solvers.Count; i++)
                {
                    runs.Add(new BenchmarkSolverRun(i, config.Solvers[i].Name, double.IsNaN(v) ? (double?)null : v));
                }
            }

            // How many total "solver-games" we need — one per game per solver
            // per axis value. Progress reports use this so a sweep of 3 axis
            // values × 2 solvers × 100 games shows correctly as "300 of 600".
            var totalGamesAcrossAxes = config.GameCount * axisValues.Length;

            for (var axisIdx = 0; axisIdx < axisValues.Length; axisIdx++)
            {
                var axisValue = axisValues[axisIdx];
                var effectiveConfig = ApplySweep(config, axisValue);

                for (var gameIdx = 0; gameIdx < config.GameCount; gameIdx++)
                {
                    // Cancellation is polled between games and between solvers — mid-game
                    // cancellation would need cooperation from ExtSolver, which we don't
                    // have. A single solve on a reasonable board finishes in hundreds of
                    // ms so this granularity is acceptable.
                    if (shouldStop?.Invoke() == true) return runs;

                    var snapSw = Stopwatch.StartNew();
                    var snapshot = GenerateSnapshot(effectiveConfig);
                    TotalSnapshotMs += snapSw.Elapsed.TotalMilliseconds;

                    for (var solverIdx = 0; solverIdx < config.Solvers.Count; solverIdx++)
                    {
                        if (shouldStop?.Invoke() == true) return runs;

                        // In a SolverParameter sweep we can't mutate the user's
                        // configured settings — clone per-iteration with the
                        // picked property overridden to the current axis value.
                        var settings = config.SweepAxis == BenchmarkSweepAxis.SolverParameter
                                       && !double.IsNaN(axisValue)
                                       && !string.IsNullOrEmpty(config.SweepParameterName)
                            ? OverrideSetting(config.Solvers[solverIdx].Settings, config.SweepParameterName, axisValue)
                            : config.Solvers[solverIdx].Settings;
                        var result = PlayOneGame(snapshot, settings, gameIdx);
                        var runIdxInList = axisIdx * config.Solvers.Count + solverIdx;
                        var run = runs[runIdxInList];
                        run.Games.Add(result);
                        switch (result.Outcome)
                        {
                            case BenchmarkOutcome.Won: run.Won++; break;
                            case BenchmarkOutcome.Lost: run.Lost++; break;
                            case BenchmarkOutcome.Stuck: run.Stuck++; break;
                        }
                        run.TotalMs += result.ElapsedMs;
                        run.TotalIterations += result.Iterations;

                        var gamesCompletedOverall = axisIdx * config.GameCount + gameIdx + 1;
                        onProgress?.Invoke(new BenchmarkProgressUpdate(gamesCompletedOverall, totalGamesAcrossAxes, solverIdx, result, runs));
                    }
                }
            }

            return runs;
        }

        // Clones the config with only the sweep axis's field replaced by the
        // current axis value. Non-sweep case (NaN) just passes the original
        // settings through. Density values are clamped to (0, 1) so a bad
        // sweep input can't crash the map generator.
        private static BenchmarkConfig ApplySweep(BenchmarkConfig config, double axisValue)
        {
            if (double.IsNaN(axisValue)) return config;
            var clone = new BenchmarkConfig
            {
                Width = config.Width,
                Height = config.Height,
                MineDensity = config.MineDensity,
                GameCount = config.GameCount,
                Solvers = config.Solvers,
                SweepAxis = BenchmarkSweepAxis.None
            };
            switch (config.SweepAxis)
            {
                case BenchmarkSweepAxis.Width: clone.Width = Math.Max(1, (int)Math.Round(axisValue)); break;
                case BenchmarkSweepAxis.Height: clone.Height = Math.Max(1, (int)Math.Round(axisValue)); break;
                case BenchmarkSweepAxis.MineDensity: clone.MineDensity = Math.Min(0.99, Math.Max(0.01, axisValue)); break;
                // SolverParameter sweeps leave board dims alone — the per-solver
                // settings clone happens inside the inner loop via OverrideSetting.
            }
            return clone;
        }

        // Shallow-copies a settings instance via reflection and writes axisValue
        // into the named property. Only int/long/double/bool properties are
        // supported; unknown/unsupported names fall through as a no-op clone.
        private static BorderSeparationSolverSettings OverrideSetting(BorderSeparationSolverSettings src, string propName, double value)
        {
            var clone = new BorderSeparationSolverSettings();
            foreach (var p in typeof(BorderSeparationSolverSettings).GetProperties())
            {
                if (!p.CanRead || !p.CanWrite) continue;
                p.SetValue(clone, p.GetValue(src));
            }
            var target = typeof(BorderSeparationSolverSettings).GetProperty(propName);
            if (target == null || !target.CanWrite) return clone;
            var t = target.PropertyType;
            object converted;
            if (t == typeof(int)) converted = (int)Math.Round(value);
            else if (t == typeof(long)) converted = (long)Math.Round(value);
            else if (t == typeof(double)) converted = value;
            else if (t == typeof(bool)) converted = value != 0;
            else return clone;
            target.SetValue(clone, converted);
            return clone;
        }

        // Generates one random board and captures enough state (player view +
        // mine layout) to rebuild a fresh engine for every solver to run on.
        // Opens the centre cell — same no-bomb-first-click convention used by
        // the main window's Generate button.
        private static BoardSnapshot GenerateSnapshot(BenchmarkConfig config)
        {
            var engine = new GameManager(new GameMapGenerator(), new GameEngine());
            engine.StartWithMineDensity(
                config.Width, config.Height,
                new Coordinate(config.Width / 2, config.Height / 2),
                true, config.MineDensity);

            var gm = engine.CurrentMap;
            var mines = new bool[gm.Width, gm.Height];
            for (var i = 0; i < gm.Width; i++)
                for (var j = 0; j < gm.Height; j++)
                    mines[i, j] = gm.Cells[i, j].HasMine;

            return new BoardSnapshot
            {
                Width = gm.Width,
                Height = gm.Height,
                PlayerView = gm.ToRegularMap(),
                Mines = mines
            };
        }

        private BenchmarkGameResult PlayOneGame(BoardSnapshot snapshot, BorderSeparationSolverSettings settings, int gameIdx)
        {
            var result = new BenchmarkGameResult { GameIndex = gameIdx };
            var sw = Stopwatch.StartNew();

            var buildSw = Stopwatch.StartNew();
            var engine = BuildEngineFromSnapshot(snapshot);
            TotalEngineBuildMs += buildSw.Elapsed.TotalMilliseconds;

            var initSw = Stopwatch.StartNew();
            if (UseDirectSolver) DirectSolver.Instance.InitSolver(settings);
            else ExtSolver.Instance.InitSolver(settings);
            TotalInitMs += initSw.Elapsed.TotalMilliseconds;

            var initialOpened = CountOpened(engine.CurrentMap);

            for (var iter = 0; iter < MaxIterationsPerGame; iter++)
            {
                var view = engine.CurrentMap.ToRegularMap();
                var solveSw = Stopwatch.StartNew();
                var results = UseDirectSolver
                    ? DirectSolver.Instance.Solve(view)
                    : ExtSolver.Instance.Solve(view);
                TotalSolverMs += solveSw.Elapsed.TotalMilliseconds;
                TotalSolveCalls++;

                // Guesser fallback mirrors AI.AI.Solve / SolveMap in MainWindow so
                // the benchmark exercises the same play path the user sees.
                if (!results.Any(x => x.Value.Verdict.HasValue))
                {
                    var guessSw = Stopwatch.StartNew();
                    var guess = new LowestProbabilityGuesser().Guess(view, results);
                    TotalGuesserMs += guessSw.Elapsed.TotalMilliseconds;
                    if (guess != null) results[guess.Coordinate] = guess;
                }

                if (results.Count == 0)
                {
                    // Nothing to do. Either fully solved or genuinely stuck.
                    result.Outcome = AllSafeCellsOpened(engine.CurrentMap) ? BenchmarkOutcome.Won : BenchmarkOutcome.Stuck;
                    result.Iterations = iter;
                    break;
                }

                var opsSw = Stopwatch.StartNew();
                var hitMine = false;
                foreach (var r in results)
                {
                    switch (r.Value.Verdict)
                    {
                        case true:
                            engine.SetFlag(r.Key, CellFlag.HasMine);
                            break;
                        case false:
                            if (!engine.OpenCell(r.Key).OpenCorrect)
                            {
                                hitMine = true;
                            }
                            break;
                    }
                    if (hitMine) break;
                }
                TotalEngineOpsMs += opsSw.Elapsed.TotalMilliseconds;

                if (hitMine)
                {
                    result.Outcome = BenchmarkOutcome.Lost;
                    result.Iterations = iter + 1;
                    break;
                }

                if (AllSafeCellsOpened(engine.CurrentMap))
                {
                    result.Outcome = BenchmarkOutcome.Won;
                    result.Iterations = iter + 1;
                    break;
                }

                // Safety net for iteration cap — treat a non-terminating solver
                // as stuck so one bad config can't run forever.
                if (iter == MaxIterationsPerGame - 1)
                {
                    result.Outcome = BenchmarkOutcome.Stuck;
                    result.Iterations = MaxIterationsPerGame;
                }
            }

            sw.Stop();
            result.ElapsedMs = sw.Elapsed.TotalMilliseconds;
            result.CellsOpened = CountOpened(engine.CurrentMap) - initialOpened;
            return result;
        }

        // Rebuilds a fresh GameMap + engine from the snapshot so every solver
        // sees an identical starting position. Hints are recomputed from the
        // mine layout rather than copied, matching BuildEngineFromState in
        // MainWindow.
        private static GameManager BuildEngineFromSnapshot(BoardSnapshot snap)
        {
            var gm = new GameMap(snap.Width, snap.Height, 0, null, false);
            for (var i = 0; i < snap.Width; i++)
            {
                for (var j = 0; j < snap.Height; j++)
                {
                    var cell = snap.PlayerView.Cells[i, j];
                    var hasMine = snap.Mines[i, j];
                    var state = cell?.State ?? CellState.Filled;
                    var flag = cell?.Flag ?? CellFlag.None;
                    gm.Cells[i, j] = new GameCell(new Coordinate(i, j), hasMine, state, flag, 0);
                }
            }
            foreach (var gc in gm.AllCells)
            {
                gc.Hint = gc.HasMine ? 0 : gm.CalculateNeighboursOf(gc.Coordinate).Count(n => n.HasMine);
            }
            gm.RemainingMineCount = gm.AllCells.Count(c => c.HasMine);
            var engine = new GameManager(new GameMapGenerator(), new GameEngine());
            engine.Start(gm);
            return engine;
        }

        private static bool AllSafeCellsOpened(GameMap gm)
            => !gm.AllCells.Any(c => !c.HasMine && c.State == CellState.Filled);

        private static int CountOpened(GameMap gm)
            => gm.AllCells.Count(c => c.State == CellState.Empty);

        private sealed class BoardSnapshot
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public Map PlayerView { get; set; }
            public bool[,] Mines { get; set; }
        }
    }

    internal sealed class BenchmarkProgressUpdate
    {
        public BenchmarkProgressUpdate(int gamesCompleted, int totalGames, int solverIndex, BenchmarkGameResult lastResult, IReadOnlyList<BenchmarkSolverRun> runs)
        {
            GamesCompleted = gamesCompleted;
            TotalGames = totalGames;
            SolverIndex = solverIndex;
            LastResult = lastResult;
            Runs = runs;
        }
        public int GamesCompleted { get; }
        public int TotalGames { get; }
        public int SolverIndex { get; }
        public BenchmarkGameResult LastResult { get; }
        public IReadOnlyList<BenchmarkSolverRun> Runs { get; }
    }
}
