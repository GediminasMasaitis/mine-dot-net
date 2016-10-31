using System.Collections.Generic;
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
            var guesserResult = guesser.Guess(solverResults);
            solverResults[guesserResult.Coordinate] = guesserResult;
            return solverResults;
        }
    }
}
