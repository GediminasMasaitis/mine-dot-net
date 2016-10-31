using System.Collections.Generic;
using MineDotNet.Common;

namespace MineDotNet.AI.Solvers
{
    internal class Border
    {
        public Border(IList<Cell> cells)
        {
            Cells = cells;
            Probabilities = new Dictionary<Coordinate, decimal>();
            Verdicts = new Dictionary<Coordinate, bool>();
        }
        public IList<Cell> Cells { get; set; }
        public IList<IDictionary<Coordinate, bool>> ValidCombinations { get; set; }
        public int MinMineCount { get; set; }
        public int MaxMineCount { get; set; }
        public IDictionary<Coordinate, decimal> Probabilities { get; set; }
        public IDictionary<Coordinate, bool> Verdicts { get; set; }
        public bool SolvedFully { get; set; }
    }
}