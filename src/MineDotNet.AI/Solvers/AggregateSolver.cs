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
            Solvers = solvers;
            Behavior = SolverAggregationBehavior.GoThroughAllSolvers;
        }

        public override IDictionary<Coordinate, Verdict> Solve(Map map)
        {
            OnDebug($"Solving {map.Width}x{map.Height} map");
            if (map.RemainingMineCount.HasValue)
            {
                OnDebug($" with {map.RemainingMineCount.Value} mines remaining");
            }
            OnDebugLine(string.Empty);
            var stopwatch = new Stopwatch();
            var allResults = new Dictionary<Coordinate, Verdict>();
            foreach (var solver in Solvers)
            {
                var solverName = solver.GetType().Name;
                OnDebugLine($"Attempting {solverName}");
                stopwatch.Restart();
                var results = solver.Solve(map);
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
                map = new Map(map.AllCells.ToList());
                foreach (var result in results)
                {
                    allResults[result.Key] = result.Value;
                    map.Cells[result.Key.X, result.Key.Y] = result.Value == Verdict.HasMine ? new Cell(result.Key, CellState.Filled, CellFlag.HasMine, 0) : new Cell(result.Key, CellState.Wall, CellFlag.None, 0);
                }
                OnDebugLine(new TextMapVisualizer().VisualizeToString(map));
            }
            return allResults;
        }
    }
}