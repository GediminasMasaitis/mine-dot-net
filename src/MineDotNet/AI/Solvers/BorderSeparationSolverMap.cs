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
        public int AntiFlaggedCount { get; set; }
        public int UndecidedCount => FilledCount - FlaggedCount - AntiFlaggedCount;

        public BorderSeparationSolverMap(IMap map) : base(CloneCellsFromMap(map), map.RemainingMineCount)
        {
            var filledCells = map.AllCells.Where(x => x.State == CellState.Filled).ToList();
            FilledCount = filledCells.Count;
            FlaggedCount = filledCells.Count(x => x.Flag == CellFlag.HasMine);
            AntiFlaggedCount = filledCells.Count(x => x.Flag == CellFlag.DoesntHaveMine);
        }

        private static IList<Cell> CloneCellsFromMap(IMap map)
        {
            return map.AllCells.Select(x => x.Clone()).ToList();
        }
    }
}