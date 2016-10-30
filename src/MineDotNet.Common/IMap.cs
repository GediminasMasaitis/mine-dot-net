using System.Collections.Generic;

namespace MineDotNet.Common
{
    public interface IMap
    {
        int Width { get; }
        int Height { get; }
        Cell[,] Cells { get; }
        int? RemainingMineCount { get; set; }
        IEnumerable<Cell> AllCells { get; }
        IDictionary<Coordinate, NeighbourCacheEntry> NeighbourCache { get; }
        bool CellExists(Coordinate coord);
        IList<Cell> GetNeighboursOf(Cell cell, bool includeSelf = false);
        void BuildNeighbourCache();
    }
}