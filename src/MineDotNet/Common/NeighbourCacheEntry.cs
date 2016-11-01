using System.Collections.Generic;

namespace MineDotNet.Common
{
    public class NeighbourCacheEntry
    {
        public IList<Cell> AllNeighbours { get; set; }
        public IDictionary<CellState, IList<Cell>> ByState { get; set; }
        public IDictionary<CellFlag, IList<Cell>> ByFlag { get; set; }
    }
}