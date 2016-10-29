using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public class AggregateSolver : SolverBase
    {
        public ISolver[] Solvers { get; set; }
        public SolverAggregationBehavior Behavior { get; set; }

        public AggregateSolver(IEnumerable<ISolver> solvers):this(solvers.ToArray())
        {
            
        }

        public AggregateSolver(params ISolver[] solvers)
        {
            if (solvers == null || solvers.Length == 0)
            {
                solvers = new ISolver[]
                {
                    new SimpleSolver(), 
                    new BorderSeparationSolver(), 
                    new OptimalGuessSolver()
                };
                foreach (var solver in solvers)
                {
                    solver.Debug += OnDebug;
                }
            }
            Solvers = solvers;
            Behavior = SolverAggregationBehavior.GoThroughAllSolvers;
        }

        public override IDictionary<Coordinate, SolverResult> Solve(Map map, IDictionary<Coordinate, SolverResult> previousResults = null)
        {
            OnDebug($"Solving {map.Width}x{map.Height} map");
            if (map.RemainingMineCount.HasValue)
            {
                OnDebug($" with {map.RemainingMineCount.Value} mines remaining");
            }
            OnDebugLine(string.Empty);
            var stopwatch = new Stopwatch();
            previousResults = previousResults ?? new Dictionary<Coordinate, SolverResult>();
            var allResults = new Dictionary<Coordinate, SolverResult>(previousResults);

            foreach (var solver in Solvers)
            {
                var solverName = solver.GetType().Name;
                OnDebugLine($"Attempting {solverName}");
                stopwatch.Restart();
                var results = solver.Solve(map, allResults);
                stopwatch.Stop();
                var elapsedStr = stopwatch.Elapsed.TotalMilliseconds.ToString("#.##");
                if (results.Count > 0)
                {
                    OnDebugLine($"{solverName} succeeded, found {results.Count} results in {elapsedStr} ms.");
                }
                else
                {
                    OnDebugLine($"{solverName} failed in {elapsedStr} ms.");
                }
                
                if (Behavior == SolverAggregationBehavior.StopOnFirstResult && results.Count > 0)
                {
                    return results;
                }
                foreach (var result in results)
                {
                    allResults[result.Key] = result.Value;
                }
                map = MergeResultsIntoMap(map, allResults);
            }
            return allResults;
        }

        private Map MergeResultsIntoMap(Map map, IDictionary<Coordinate, SolverResult> previousResults)
        {
            map = new Map(map.AllCells.ToList());
            foreach (var result in previousResults)
            {
                switch (result.Value.Verdict)
                {
                    case Verdict.HasMine:
                        map.Cells[result.Key.X, result.Key.Y] = new Cell(result.Key, CellState.Filled, CellFlag.HasMine, 0);
                        break;
                    case Verdict.DoesntHaveMine:
                        map.Cells[result.Key.X, result.Key.Y] = new Cell(result.Key, CellState.Wall, CellFlag.None, 0);
                        break;
                }
            }
            OnDebugLine(new TextMapVisualizer().VisualizeToString(map));
            return map;
        }
    }
}