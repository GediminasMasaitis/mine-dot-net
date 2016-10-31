using System;
using System.Collections.Generic;
using System.Linq;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    internal class BorderSeparationSolverMap : Map
    {
        public int FilledCount { get; set; }
        public int FlaggedCount { get; set; }
        public int UndecidedCount => FilledCount - FlaggedCount;

        public BorderSeparationSolverMap(IMap map) : base(CloneCellsFromMap(map), map.RemainingMineCount)
        {
            
        }

        private static IList<Cell> CloneCellsFromMap(IMap map)
        {
            return map.AllCells.Select(x => x.Clone()).ToList();
        }
    }
}