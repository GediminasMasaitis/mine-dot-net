using System;
using System.Collections.Generic;
using System.Linq;
using MineDotNet.Common;

namespace MineDotNet.AI.Guessers
{
    class LowestProbabilityGuesser
    {
        public SolverResult Guess(IDictionary<Coordinate, SolverResult> solverResults)
        {
            if (solverResults == null) throw new ArgumentNullException(nameof(solverResults));

            var leastRiskyPrediction = solverResults.FirstOrDefault().Value;
            foreach (var solverResult in solverResults.Values)
            {
                if (solverResult.Probability < leastRiskyPrediction.Probability)
                {
                    leastRiskyPrediction = solverResult;
                }
            }
            return leastRiskyPrediction;
        }
    }
}
