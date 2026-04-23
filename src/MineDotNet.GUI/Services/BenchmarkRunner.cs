using System;
using System.Collections.Concurrent;
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

        // Runs the configured benchmark. Every enabled solver plays the same
        // board per game so win-rate deltas reflect settings, not RNG luck.
        //
        // Parallelism: when config.Parallelism > 1, games are distributed
        // across that many workers — each owning its own ExtSolver subprocess.
        // DirectSolver is a process-global singleton so with UseDirectSolver
        // every worker shares it under lock (effectively serialized). The
        // drain-and-onProgress loop runs on the calling thread so the dialog
        // can touch WPF directly from the progress callback.
        //
        // When true, benchmark calls DirectSolver (P/Invoke into the shared
        // library) instead of ExtSolver (stdio to UMSI). Set via the dialog
        // checkbox before Run(). Defaults off — ExtSolver path is the proven
        // one, direct is experimental.
        public bool UseDirectSolver { get; set; }

        public IReadOnlyList<BenchmarkSolverRun> Run(
            BenchmarkConfig config,
            Action<BenchmarkProgressUpdate> onProgress = null,
            Func<bool> shouldStop = null,
            Action onTick = null)
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

            // Total progress granularity is one tick per solver-game across all
            // axis values. Finer than per-board, and matches the rate at which
            // workers enqueue completion events.
            var totalSolverGames = config.GameCount * config.Solvers.Count * axisValues.Length;

            // Worker 0 reuses the appropriate shared singleton so the common
            // parallelism=1 case doesn't spin up extra native state. Additional
            // workers get fresh instances (own UMSI subprocess for ExtSolver,
            // own solver handle for DirectSolver) and are disposed in finally.
            // Both paths now parallelize: DirectSolver's C++ globals were
            // refactored into per-handle state (see global_wrappers.cpp).
            var workerCount = Math.Max(1, config.Parallelism);
            var solvers = new ISolver[workerCount];
            var stats = new WorkerStats[workerCount];
            var ownedDisposables = new List<IDisposable>();
            try
            {
                for (var w = 0; w < workerCount; w++)
                {
                    stats[w] = new WorkerStats();
                    if (UseDirectSolver)
                    {
                        if (w == 0)
                        {
                            solvers[w] = DirectSolver.Instance;
                        }
                        else
                        {
                            var ds = new DirectSolver();
                            solvers[w] = ds;
                            ownedDisposables.Add(ds);
                        }
                    }
                    else if (w == 0)
                    {
                        solvers[w] = ExtSolver.Instance;
                    }
                    else
                    {
                        var es = new ExtSolver();
                        solvers[w] = es;
                        ownedDisposables.Add(es);
                    }
                }

                var completed = 0;

                for (var axisIdx = 0; axisIdx < axisValues.Length; axisIdx++)
                {
                    if (shouldStop?.Invoke() == true) break;

                    var axisValue = axisValues[axisIdx];
                    var effectiveConfig = ApplySweep(config, axisValue);
                    var capturedAxisIdx = axisIdx;
                    var nextGameIdx = 0;
                    var queue = new ConcurrentQueue<ResultEvent>();
                    var workers = new Task[workerCount];

                    for (var w = 0; w < workerCount; w++)
                    {
                        var solver = solvers[w];
                        var workerStats = stats[w];
                        workers[w] = Task.Run(() =>
                        {
                            // Cancellation polled between games and between
                            // solvers — mid-solve cancellation would need the
                            // engine's cooperation.
                            while (true)
                            {
                                if (shouldStop?.Invoke() == true) return;
                                var gameIdx = Interlocked.Increment(ref nextGameIdx) - 1;
                                if (gameIdx >= config.GameCount) return;

                                var snapSw = Stopwatch.StartNew();
                                var snapshot = GenerateSnapshot(effectiveConfig);
                                workerStats.SnapshotMs += snapSw.Elapsed.TotalMilliseconds;

                                for (var solverIdx = 0; solverIdx < config.Solvers.Count; solverIdx++)
                                {
                                    if (shouldStop?.Invoke() == true) return;

                                    // In a SolverParameter sweep we can't mutate the user's
                                    // configured settings — clone per-iteration with the
                                    // picked property overridden to the current axis value.
                                    var settings = config.SweepAxis == BenchmarkSweepAxis.SolverParameter
                                                   && !double.IsNaN(axisValue)
                                                   && !string.IsNullOrEmpty(config.SweepParameterName)
                                        ? OverrideSetting(config.Solvers[solverIdx].Settings, config.SweepParameterName, axisValue)
                                        : config.Solvers[solverIdx].Settings;
                                    var result = PlayOneGame(snapshot, settings, gameIdx, solver, workerStats);
                                    queue.Enqueue(new ResultEvent(capturedAxisIdx, solverIdx, result));
                                }
                            }
                        });
                    }

                    // Drain on the calling thread so onProgress (which touches
                    // WPF in the dialog) runs on the UI thread. 30ms polling
                    // is tight enough for responsive progress without spin.
                    // onTick fires every iteration — even when the queue is
                    // empty — so the caller can keep its UI alive during long
                    // solves that don't enqueue frequently.
                    while (!Task.WaitAll(workers, 30))
                    {
                        completed = DrainQueue(queue, runs, config, totalSolverGames, completed, onProgress);
                        onTick?.Invoke();
                    }
                    completed = DrainQueue(queue, runs, config, totalSolverGames, completed, onProgress);
                    onTick?.Invoke();
                }

                // Fold per-worker timings into the runner-level totals the
                // dialog reads for the end-of-run breakdown. Values are sums
                // across workers, so at parallelism > 1 the totals can exceed
                // wall time — that's informative, not a bug.
                foreach (var s in stats)
                {
                    TotalSolverMs += s.SolverMs;
                    TotalInitMs += s.InitMs;
                    TotalSnapshotMs += s.SnapshotMs;
                    TotalEngineBuildMs += s.EngineBuildMs;
                    TotalEngineOpsMs += s.EngineOpsMs;
                    TotalGuesserMs += s.GuesserMs;
                    TotalSolveCalls += s.SolveCalls;
                }
            }
            finally
            {
                foreach (var d in ownedDisposables)
                {
                    try { d.Dispose(); } catch { /* best effort */ }
                }
            }

            return runs;
        }

        private static int DrainQueue(
            ConcurrentQueue<ResultEvent> queue,
            List<BenchmarkSolverRun> runs,
            BenchmarkConfig config,
            int totalSolverGames,
            int completed,
            Action<BenchmarkProgressUpdate> onProgress)
        {
            while (queue.TryDequeue(out var e))
            {
                var runIdx = e.AxisIdx * config.Solvers.Count + e.SolverIdx;
                var run = runs[runIdx];
                run.Games.Add(e.Result);
                switch (e.Result.Outcome)
                {
                    case BenchmarkOutcome.Won: run.Won++; break;
                    case BenchmarkOutcome.Lost: run.Lost++; break;
                    case BenchmarkOutcome.Stuck: run.Stuck++; break;
                }
                run.TotalMs += e.Result.ElapsedMs;
                run.TotalIterations += e.Result.Iterations;
                completed++;
                onProgress?.Invoke(new BenchmarkProgressUpdate(completed, totalSolverGames, e.SolverIdx, e.Result, runs));
            }
            return completed;
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

        private BenchmarkGameResult PlayOneGame(BoardSnapshot snapshot, BorderSeparationSolverSettings settings, int gameIdx, ISolver solver, WorkerStats stats)
        {
            var result = new BenchmarkGameResult { GameIndex = gameIdx };
            var sw = Stopwatch.StartNew();

            var buildSw = Stopwatch.StartNew();
            var engine = BuildEngineFromSnapshot(snapshot);
            stats.EngineBuildMs += buildSw.Elapsed.TotalMilliseconds;

            var initSw = Stopwatch.StartNew();
            solver.InitSolver(settings);
            stats.InitMs += initSw.Elapsed.TotalMilliseconds;

            var initialOpened = CountOpened(engine.CurrentMap);

            for (var iter = 0; iter < MaxIterationsPerGame; iter++)
            {
                var view = engine.CurrentMap.ToRegularMap();
                var solveSw = Stopwatch.StartNew();
                var results = solver.Solve(view);
                stats.SolverMs += solveSw.Elapsed.TotalMilliseconds;
                stats.SolveCalls++;

                // Guesser fallback mirrors AI.AI.Solve / SolveMap in MainWindow so
                // the benchmark exercises the same play path the user sees.
                if (!results.Any(x => x.Value.Verdict.HasValue))
                {
                    var guessSw = Stopwatch.StartNew();
                    var guess = new LowestProbabilityGuesser().Guess(view, results);
                    stats.GuesserMs += guessSw.Elapsed.TotalMilliseconds;
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
                stats.EngineOpsMs += opsSw.Elapsed.TotalMilliseconds;

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

        // Per-worker timing bucket. Only the owning worker writes to its
        // instance, so no synchronization is needed during the run; totals
        // are folded into the runner's fields after all workers finish.
        private sealed class WorkerStats
        {
            public double SolverMs;
            public double InitMs;
            public double SnapshotMs;
            public double EngineBuildMs;
            public double EngineOpsMs;
            public double GuesserMs;
            public int SolveCalls;
        }

        // Carries a single solver-on-board result from a worker back to the
        // drain loop. AxisIdx identifies the sweep slot, SolverIdx identifies
        // which configured solver produced the result.
        private sealed class ResultEvent
        {
            public ResultEvent(int axisIdx, int solverIdx, BenchmarkGameResult result)
            {
                AxisIdx = axisIdx;
                SolverIdx = solverIdx;
                Result = result;
            }
            public int AxisIdx { get; }
            public int SolverIdx { get; }
            public BenchmarkGameResult Result { get; }
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
