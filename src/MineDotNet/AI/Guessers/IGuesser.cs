using System.Collections.Generic;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;

namespace MineDotNet.AI.Guessers
{
    public interface IGuesser
    {
        SolverResult Guess(IMap map, IDictionary<Coordinate, SolverResult> solverResults);
    }
}