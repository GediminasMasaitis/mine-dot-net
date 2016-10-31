using System;
using System.Collections.Generic;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public interface ISolver
    {
#if DEBUG
        event Action<string> Debug;
#endif
        IDictionary<Coordinate, SolverResult> Solve(IMap map);
    }
}