using System.Collections.Generic;
using MineDotNet.Game;
using MineDotNet.Game.Models;

namespace MineDotNet.Common
{
    public interface IMap : IMapBase<Cell>
    {
        IDictionary<Coordinate, NeighbourCacheEntry> NeighbourCache { get; }
        bool CellExists(Coordinate coord);
        //IList<Cell> GetNeighboursOf(Coordinate coord, bool includeSelf = false);
        void BuildNeighbourCache();
    }

    public interface IMapBase<TCell> : IReadOnlyMapBase<TCell>
        where TCell : Cell
    {
        new int? RemainingMineCount { get; set; }
        TCell[,] Cells { get; }
        new TCell this[Coordinate coordinate] { get; set; }
    }

    public interface IGameMap : IMapBase<GameCell>
    {
    }

    public interface IReadOnlyMapBase<out TCell>
        where TCell : Cell
    {
        int Width { get; }
        int Height { get; }
        int? RemainingMineCount { get; }
        IEnumerable<TCell> AllCells { get; }

        TCell this[Coordinate coordinate] { get; }
    }
}