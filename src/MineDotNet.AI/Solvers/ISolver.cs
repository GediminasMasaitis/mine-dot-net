using System;
using System.Collections.Generic;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public interface ISolver
    {
        event Action<string> Debug;
        IDictionary<Coordinate, SolverResult> Solve(Map map);
    }
}