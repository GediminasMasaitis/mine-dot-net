using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    class OptimalGuessSolver : SolverBase
    {
        public override IDictionary<Coordinate, SolverResult> Solve(IMap map, IDictionary<Coordinate, SolverResult> previousResults = null)
        {
            var guesses = new Dictionary<Coordinate, SolverResult>();
            if (previousResults == null || previousResults.Any(x => x.Value.Verdict.HasValue))
            {
                return guesses;
            }

            var leastRiskyPrediction = previousResults.MinBy(x => x.Value.Probability);
            var chanceStr = (1 - leastRiskyPrediction.Value.Probability).ToString("##0%");
            OnDebugLine("Guessing with " + chanceStr + " chance of success.");
            var guess = new SolverResult(leastRiskyPrediction.Key, leastRiskyPrediction.Value.Probability, Verdict.DoesntHaveMine);
            guesses[guess.Coordinate] = guess;
            return guesses;
        }
    }
}
