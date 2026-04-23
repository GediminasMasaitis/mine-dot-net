using System;
using System.Collections.Generic;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    public interface ISolver
    {
        void InitSolver(BorderSeparationSolverSettings settings);
        IDictionary<Coordinate, SolverResult> Solve(IMap map);
    }
}