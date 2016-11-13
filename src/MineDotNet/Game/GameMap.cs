using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.Game
{
    public class GameMap
    {
        public int Width { get; }
        public int Height { get; }
        public int? RemainingMineCount { get; set; }
        public GameCell[,] Cells { get; private set; }
        public IEnumerable<GameCell> AllCells => Cells.Cast<GameCell>().Where(x => x != null);

        public GameMap(int width, int height, int? remainingMineCount = null, bool createCells = false, CellState fillWithState = CellState.Empty)
        {
            Width = width;
            Height = height;
            Cells = new GameCell[Width, Height];
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    var cell = createCells ? new GameCell(i, j, false, fillWithState) : null;
                    Cells[i, j] = cell;
                }
            }
            RemainingMineCount = remainingMineCount;
        }

        public GameCell this[Coordinate coordinate]
        {
            get { return Cells[coordinate.X, coordinate.Y]; }
            set { Cells[coordinate.X, coordinate.Y] = value; }
        }
        public bool CellExists(Coordinate coord) => coord.X >= 0 && coord.Y >= 0 && coord.X < Width && coord.Y < Height && Cells[coord.X, coord.Y] != null && Cells[coord.X, coord.Y].State != CellState.Wall;

        public IList<GameCell> CalculateNeighboursOf(Coordinate coord, bool includeSelf = false)
        {
            var validOffsets = Map.NeighbourOffsets.Select(x => coord + x).Where(CellExists).ToList();
            if (includeSelf && CellExists(coord))
            {
                validOffsets.Add(coord);
            }
            var neighbours = validOffsets.Select(x => Cells[x.X, x.Y]).ToList();
            return neighbours;
        }

        public Map ToRegularMap()
        {
            var cellCopy = AllCells.Select(x => new Cell(x.Coordinate, x.State, x.Flag, x.Hint)).ToList();
            foreach (var cell in cellCopy.Where(x => x.State != CellState.Empty))
            {
                cell.Hint = 0;
            }
            return new Map(cellCopy, RemainingMineCount);
        }

        public GameMap Clone()
        {
            var cellCopy = AllCells.Select(x => new GameCell(x.Coordinate, x.HasMine, x.State, x.Flag, x.Hint)).ToList();
            var map = new GameMap(Width, Height, RemainingMineCount);
            foreach (var cell in cellCopy)
            {
                map[cell.Coordinate] = cell;
            }
            return map;
        }
    }

}
