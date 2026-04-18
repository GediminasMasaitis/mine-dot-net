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

        // Runs the configured benchmark end-to-end on a background thread.
        // Each game generates one board; every enabled solver plays the same
        // board so win-rate deltas reflect settings, not RNG luck.
        //
        // progress fires after each solver-on-board completes so the UI can
        // tick counters between board regenerations. The returned list is
        // always in solver-index order and is already final when the task
        // completes.
        public Task<IReadOnlyList<BenchmarkSolverRun>> RunAsync(
            BenchmarkConfig config,
            IProgress<BenchmarkProgressUpdate> progress,
            CancellationToken cancel)
        {
            return Task.Run(() =>
            {
                var runs = config.Solvers
                    .Select((s, i) => new BenchmarkSolverRun(i, s.Name))
                    .ToList();

                for (var gameIdx = 0; gameIdx < config.GameCount; gameIdx++)
                {
                    if (cancel.IsCancellationRequested) break;

                    var snapshot = GenerateSnapshot(config);

                    for (var solverIdx = 0; solverIdx < config.Solvers.Count; solverIdx++)
                    {
                        if (cancel.IsCancellationRequested) break;
                        var result = PlayOneGame(snapshot, config.Solvers[solverIdx].Settings, gameIdx);
                        var run = runs[solverIdx];
                        run.Games.Add(result);
                        switch (result.Outcome)
                        {
                            case BenchmarkOutcome.Won: run.Won++; break;
                            case BenchmarkOutcome.Lost: run.Lost++; break;
                            case BenchmarkOutcome.Stuck: run.Stuck++; break;
                        }
                        run.TotalMs += result.ElapsedMs;
                        run.TotalIterations += result.Iterations;

                        progress?.Report(new BenchmarkProgressUpdate(gameIdx + 1, config.GameCount, solverIdx, result));
                    }
                }

                return (IReadOnlyList<BenchmarkSolverRun>)runs;
            }, cancel);
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

        private static BenchmarkGameResult PlayOneGame(BoardSnapshot snapshot, BorderSeparationSolverSettings settings, int gameIdx)
        {
            var result = new BenchmarkGameResult { GameIndex = gameIdx };
            var sw = Stopwatch.StartNew();

            var engine = BuildEngineFromSnapshot(snapshot);
            ExtSolver.Instance.InitSolver(settings);

            var initialOpened = CountOpened(engine.CurrentMap);

            for (var iter = 0; iter < MaxIterationsPerGame; iter++)
            {
                var view = engine.CurrentMap.ToRegularMap();
                var results = ExtSolver.Instance.Solve(view);

                // Guesser fallback mirrors AI.AI.Solve / SolveMap in MainWindow so
                // the benchmark exercises the same play path the user sees.
                if (!results.Any(x => x.Value.Verdict.HasValue))
                {
                    var guess = new LowestProbabilityGuesser().Guess(view, results);
                    if (guess != null) results[guess.Coordinate] = guess;
                }

                if (results.Count == 0)
                {
                    // Nothing to do. Either fully solved or genuinely stuck.
                    result.Outcome = AllSafeCellsOpened(engine.CurrentMap) ? BenchmarkOutcome.Won : BenchmarkOutcome.Stuck;
                    result.Iterations = iter;
                    break;
                }

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
        public BenchmarkProgressUpdate(int gamesCompleted, int totalGames, int solverIndex, BenchmarkGameResult lastResult)
        {
            GamesCompleted = gamesCompleted;
            TotalGames = totalGames;
            SolverIndex = solverIndex;
            LastResult = lastResult;
        }
        public int GamesCompleted { get; }
        public int TotalGames { get; }
        public int SolverIndex { get; }
        public BenchmarkGameResult LastResult { get; }
    }
}
