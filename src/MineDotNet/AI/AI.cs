using System.Collections.Generic;
using System.Linq;
using MineDotNet.AI.Guessers;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;

namespace MineDotNet.AI
{
    public static class AI
    {
        public static IDictionary<Coordinate, SolverResult> Solve(IMap map)
        {
            var solver = new BorderSeparationSolver();
            var guesser = new LowestProbabilityGuesser();
            var solverResults = solver.Solve(map);
            if (solverResults.Any(x => x.Value.Verdict.HasValue))
            {
                return solverResults;
            }
            var guesserResult = guesser.Guess(map, solverResults);
            if (guesserResult != null)
            {
                solverResults[guesserResult.Coordinate] = guesserResult;
            }
            return solverResults;
        }
    }
}
