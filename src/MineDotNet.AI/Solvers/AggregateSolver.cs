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
            Behavior = SolverAggregationBehavior.StopOnFirstResult;
        }

        public override IDictionary<Coordinate, Verdict> Solve(Map map)
        {
            OnDebugLine("Solving " + map.Width + "x" + map.Height + " map.");
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
                allResults.AddRange(results);
            }
            return allResults;
        }
    }
}