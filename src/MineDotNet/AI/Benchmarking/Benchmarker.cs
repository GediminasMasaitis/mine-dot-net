using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MineDotNet.AI.Guessers;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game;

namespace MineDotNet.AI.Benchmarking
{
    public class Benchmarker
    {
        public event Action<Map, IDictionary<Coordinate, SolverResult>, SolverResult> SolverStep;
        public event Action<Map, IDictionary<Coordinate, SolverResult>> MissingFromPrimary;
        public event Action<Map, IDictionary<Coordinate, SolverResult>> MissingFromSecondary;
        public event Action<BenchmarkEntry, BenchmarkEntry> OneSolverFailed;
        public event Action<BenchmarkEntry> AfterBenchmark;

        public bool AllowParallelBenchmarking { get; set; }

        /*private IList<GameEngine> InitEngines(int width, int height, double mineDensity, int testsToRun)
        {
            return InitEngines(width, height, (int)(width * height / mineDensity), testsToRun);
        }

        private IEnumerable<GameMap> InitEngines(int width, int height, int mineCount, int testsToRun)
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
        }*/

        public BenchmarkDensityGroup BenchmarkWithMineCount(ISolver solver, IGuesser guesser, int width, int height, int mineCount, int testsToRun, ISolver secondarySolver = null)
        {
            //var engines = InitEngines(width, height, mineCount, testsToRun);
            var random = new Random(0);
            var generator = new GameMapGenerator(random);
            var startingPos = new Coordinate(width / 2, height / 2);
            var maps = generator.GenerateSequenceWithMineCount(width, height, startingPos, true, mineCount).Take(testsToRun);
            var entries = new List<BenchmarkEntry>();
            var i = 0;
            var sync = new object();
            void BenchmarkMapOuter(GameMap map)
            {
                var originalMap = map.Clone();
                var entry = BenchmarkMap(map, solver, guesser, secondarySolver);
                entry.GameMap = originalMap;
                if (secondarySolver != null)
                {
                    var secondaryMap = originalMap.Clone();
                    var secondaryEntry = BenchmarkMap(secondaryMap, secondarySolver, guesser, null);
                    if (entry.Solved != secondaryEntry.Solved)
                    {
                        OneSolverFailed?.Invoke(entry, secondaryEntry);
                    }
                }
                lock (sync)
                {
                    entry.Index = i;
                    AfterBenchmark?.Invoke(entry);
                    entries.Add(entry);
                    i++;
                }
            }

            if (AllowParallelBenchmarking)
            {
                Parallel.ForEach(maps, BenchmarkMapOuter);
            }
            else
            {
                foreach(var map in maps)
                {
                    BenchmarkMapOuter(map);
                }
            }
            var group = new BenchmarkDensityGroup(entries, mineCount/(double) (width*height));
            return group;
        }

        public BenchmarkDensityGroup BenchmarkWithMineDensity(ISolver solver, IGuesser guesser, int width, int height, double density, int testsToRun, ISolver secondarySolver = null)
        {
            return BenchmarkWithMineCount(solver, guesser, width, height, (int) (width*height*density), testsToRun, secondarySolver);
        }

        public IEnumerable<BenchmarkDensityGroup> BenchmarkMultipleDensities(ISolver solver, IGuesser guesser, int width, int height, double minDensity, double maxDensity, double densityInterval, int testsToRun, ISolver secondarySolver = null)
        {
            for (var currentDensity = minDensity; currentDensity <= maxDensity; currentDensity += densityInterval)
            {
                yield return BenchmarkWithMineDensity(solver, guesser, width, height, currentDensity, testsToRun, secondarySolver);
            }
        }

        public BenchmarkEntry BenchmarkMap(GameMap gameMap, ISolver solver, IGuesser guesser, ISolver secondarySolver)
        {
            var entry = new BenchmarkEntry();
            entry.GameMap = gameMap;
            entry.MineCount = entry.GameMap.RemainingMineCount;
            var engine = new GameEngine();
            engine.Start(gameMap);
            var sw = new Stopwatch();
            while (true)
            {
                var map = gameMap.ToRegularMap();
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
                SolverResult guesserResult = null;
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
                SolverStep?.Invoke(map, solverResults, guesserResult);
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
