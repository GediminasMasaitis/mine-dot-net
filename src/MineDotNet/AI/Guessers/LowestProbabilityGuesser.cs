﻿using System;
using System.Collections.Generic;
using System.Linq;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;

namespace MineDotNet.AI.Guessers
{
    public class LowestProbabilityGuesser : IGuesser
    {
        public SolverResult Guess(IMap map, IDictionary<Coordinate, SolverResult> solverResults)
        {
            if (solverResults == null) throw new ArgumentNullException(nameof(solverResults));

            var sortedResults = solverResults.OrderBy(x => x.Key.X).ThenBy(x => x.Key.Y).ToDictionary(x => x.Key, x=> x.Value);

            var leastRiskyPrediction = sortedResults.FirstOrDefault().Value;
            foreach (var solverResult in sortedResults.Values)
            {
                if (solverResult.Probability < leastRiskyPrediction.Probability)
                {
                    leastRiskyPrediction = solverResult;
                }
            }
            if (leastRiskyPrediction == null)
            {
                var cell = map.AllCells.FirstOrDefault(x => x.State == CellState.Filled && x.Flag == CellFlag.None);
                if (cell == null)
                {
                    return null;
                }
                return new SolverResult(cell.Coordinate, 0, false);
            }
            var guess = new SolverResult(leastRiskyPrediction.Coordinate, leastRiskyPrediction.Probability, false);
            return guess;
        }
    }
}
