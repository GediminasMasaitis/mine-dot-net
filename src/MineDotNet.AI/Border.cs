using System.Collections.Generic;
using MineDotNet.Common;

namespace MineDotNet.AI
{
    public class Border
    {
        public Border(IList<Cell> cells)
        {
            Cells = cells;
        }
        public IList<Cell> Cells { get; set; }
        public IList<IDictionary<Coordinate, Verdict>> ValidCombinations { get; set; }
        public int MinMineCount { get; set; }
        public int MaxMineCount { get; set; }
        public IDictionary<Coordinate, decimal> Probabilities { get; set; }
        public bool SolvedFully { get; set; }
    }
}