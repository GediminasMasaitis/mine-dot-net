using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MineDotNet.AI.Guessers;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game;

namespace MineDotNet.AI.Benchmarking
{
    public class Benchmarker
    {
        public event Action<Map, IDictionary<Coordinate, SolverResult>> MissingFromPrimary;
        public event Action<Map, IDictionary<Coordinate, SolverResult>> MissingFromSecondary;
        public event Action<BenchmarkEntry> AfterBenchmark;

        private IList<GameEngine> InitEngines(int width, int height, double mineDensity, int testsToRun)
        {
            return InitEngines(width, height, (int)(width * height / mineDensity), testsToRun);
        }

        private IList<GameEngine> InitEngines(int width, int height, int mineCount, int testsToRun)
        {
            var random = new Random(0);
            var generator = new GameMapGenerator(random);
            var engines = new List<GameEngine>(testsToRun);
            var startingPos = new Coordinate(width / 2, height / 2);
            for (var i = 0; i < testsToRun; i++)
            {
                var engine = new GameEngine(generator);
                engine.StartNew(width, height, startingPos, true, mineCount);
                engines.Add(engine);
            }
            return engines;
        }

        public BenchmarkDensityGroup Benchmark(ISolver solver, IGuesser guesser, int width, int height, int mineCount, int testsToRun, ISolver secondarySolver = null)
        {
            var engines = InitEngines(width, height, mineCount, testsToRun);
            var entries = engines.Select(x =>
            {
                var result = BenchmarkEngine(x, solver, guesser, secondarySolver);
                AfterBenchmark?.Invoke(result);
                return result;
            });
            var group = new BenchmarkDensityGroup(entries, mineCount/(double) (width*height));
            return group;
        }

        public BenchmarkDensityGroup Benchmark(ISolver solver, IGuesser guesser, int width, int height, double density, int testsToRun, ISolver secondarySolver = null)
        {
            return Benchmark(solver, guesser, width, height, (int) (width*height*density), testsToRun, secondarySolver);
        }

        public IEnumerable<BenchmarkDensityGroup> BenchmarkMultipleDensities(ISolver solver, IGuesser guesser, int width, int height, double minDensity, double maxDensity, double densityInterval, int testsToRun, ISolver secondarySolver = null)
        {
            for (var currentDensity = minDensity; currentDensity <= maxDensity; currentDensity += densityInterval)
            {
                yield return Benchmark(solver, guesser, width, height, currentDensity, testsToRun, secondarySolver);
            }
        }

        private BenchmarkEntry BenchmarkEngine(GameEngine engine, ISolver solver, IGuesser guesser, ISolver secondarySolver)
        {
            var entry = new BenchmarkEntry();
            entry.GameMap = engine.GameMap;
            entry.MineCount = entry.GameMap.RemainingMineCount.Value;
            var sw = new Stopwatch();
            while (true)
            {
                var map = engine.GameMap.ToRegularMap();
                sw.Restart();
                var solverResults = solver.Solve(map);
                sw.Stop();
                if (secondarySolver != null)
                {
                    var secondarySolverResults = secondarySolver.Solve(map);
                    var missingPrimaryResults = secondarySolverResults.Where(x => x.Value.Verdict.HasValue && !solverResults.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                    var missingSecondaryResults = solverResults.Where(x => x.Value.Verdict.HasValue && !secondarySolverResults.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => x.Value);
                    if (missingPrimaryResults.Count > 0)
                    {
                        MissingFromPrimary?.Invoke(map, missingPrimaryResults);
                    }
                    if (missingSecondaryResults.Count > 0)
                    {
                        MissingFromSecondary?.Invoke(map, missingSecondaryResults);
                    }
                }
                entry.SolvingDuarations.Add(sw.Elapsed);
                entry.TotalDuration += sw.Elapsed;
                var resultsWithVerdicts = solverResults.Where(x => x.Value.Verdict.HasValue).ToDictionary(x => x.Key, x => x.Value);
                SolverResult guesserResult;
                if (resultsWithVerdicts.Count == 0)
                {
                    guesserResult = guesser.Guess(map, solverResults);
                    if (guesserResult != null)
                    {
                        resultsWithVerdicts[guesserResult.Coordinate] = guesserResult;
                    }
                    else
                    {
                        entry.Solved = true;
                        return entry;
                    }
                }

                foreach (var result in resultsWithVerdicts)
                {
                    switch (result.Value.Verdict.Value)
                    {
                        case true:
                            engine.SetFlag(result.Key, CellFlag.HasMine);
                            break;
                        case false:
                            var success = engine.OpenCell(result.Key);
                            if (!success)
                            {
                                entry.Solved = false;
                                return entry;
                            }
                            break;
                    }
                }
            }
        }
    }
}
