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
    public class BenchmarkEntry
    {
        public BenchmarkEntry()
        {
            SolvingDuarations = new List<TimeSpan>();
        }

        public GameMap GameMap { get; set; }
        public IList<TimeSpan> SolvingDuarations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool Solved { get; set; }
    }

    public class Benchmarker
    {
        public IList<GameEngine> Engines { get; }

        public Benchmarker(int width, int height, double mineDensity, int testsToRun)
        {
            var random = new Random(0);
            var generator = new GameMapGenerator(random);
            Engines = new List<GameEngine>(testsToRun);
            var startingPos = new Coordinate(width/2, height/2);
            for (var i = 0; i < testsToRun; i++)
            {
                var engine = new GameEngine(generator);
                engine.StartNew(width, height, startingPos, true, mineDensity);
                Engines.Add(engine);
            }
        }

        public IEnumerable<BenchmarkEntry> Benchmark(ISolver solver, IGuesser guesser)
        {
            return Engines.Select(x => BenchmarkEngine(x, solver, guesser));
        }

        private BenchmarkEntry BenchmarkEngine(GameEngine engine, ISolver solver, IGuesser guesser)
        {
            var entry = new BenchmarkEntry();
            var sw = new Stopwatch();
            while (true)
            {
                var map = engine.GameMap.ToRegularMap();
                sw.Restart();
                var solverResults = solver.Solve(map);
                sw.Stop();
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
