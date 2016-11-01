using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace MineDotNet.Common
{
    public class Map : IMap
    {
        public int? RemainingMineCount { get; set; }
        public int Width { get; }
        public int Height { get; }

        public Cell[,] Cells { get; }

        public IEnumerable<Cell> AllCells => Cells.Cast<Cell>().Where(x => x != null);

        public IDictionary<Coordinate, NeighbourCacheEntry> NeighbourCache { get; private set; }

        public Map(int width, int height, int? remainingMineCount = null, bool createCells = false, CellState fillWithState = CellState.Empty)
        {
            Width = width;
            Height = height;
            Cells = new Cell[Width, Height];
            for (var i = 0; i < Width; i++)
            {
                for (var j = 0; j < Height; j++)
                {
                    var cell = createCells ? new Cell(i, j, fillWithState) : null;
                    Cells[i, j] = cell;
                }
            }
            RemainingMineCount = remainingMineCount;
        }

        public Map(IList<Cell> cells, int? remainingMineCount = null) : this(cells.Max(c => c.X)+1, cells.Max(c => c.Y)+1, remainingMineCount)
        {
            foreach (var cell in cells)
            {
                Cells[cell.X , cell.Y] = cell;
            }
        }

        public static readonly ReadOnlyCollection<Coordinate> NeighbourOffsets = new ReadOnlyCollection<Coordinate>(new List<Coordinate>{
            new Coordinate(-1,-1),
            new Coordinate(-1,0),
            new Coordinate(-1,1),
            new Coordinate(0,-1),
            new Coordinate(0,1),
            new Coordinate(1,-1),
            new Coordinate(1,0),
            new Coordinate(1,1)
        });

        public bool CellExists(Coordinate coord) => coord.X >= 0 && coord.Y >= 0 && coord.X < Width && coord.Y < Height && Cells[coord.X, coord.Y] != null && Cells[coord.X, coord.Y].State != CellState.Wall;

        public void BuildNeighbourCache()
        {
            var cache = new Dictionary<Coordinate, NeighbourCacheEntry>();
            foreach (var cell in AllCells)
            {
                var neighbours = CalculateNeighboursOf(cell.Coordinate);
                var entry = new NeighbourCacheEntry();
                entry.AllNeighbours = neighbours;

                var states = Enum.GetValues(typeof(CellState)).Cast<CellState>().ToList();
                entry.ByState = new Dictionary<CellState, IList<Cell>>(states.Count);
                foreach (var cellState in states)
                {
                    entry.ByState[cellState] = neighbours.Where(x => x.State == cellState).ToList();
                }

                var flags = Enum.GetValues(typeof(CellFlag)).Cast<CellFlag>().ToList();
                entry.ByFlag = new Dictionary<CellFlag, IList<Cell>>(flags.Count);
                foreach (var cellFlag in flags)
                {
                    entry.ByFlag[cellFlag] = neighbours.Where(x => x.Flag == cellFlag).ToList();
                }

                cache.Add(cell.Coordinate, entry);
            }
            NeighbourCache = cache;
        }

        private IList<Cell> CalculateNeighboursOf(Coordinate coord, bool includeSelf = false)
        {
            var validOffsets = NeighbourOffsets.Select(x => coord + x).Where(CellExists).ToList();
            if (includeSelf && CellExists(coord))
            {
                validOffsets.Add(coord);
            }
            var neighbours = validOffsets.Select(x => Cells[x.X, x.Y]).ToList();
            return neighbours;
        }

        public Map Transpose()
        {
            var cellList = AllCells.Select(c => new Cell(new Coordinate(c.Coordinate.Y, c.Coordinate.X), c.State, c.Flag, c.Hint)).ToList();
            return new Map(cellList);
        }

        public Cell this[Coordinate coordinate]
        {
            get { return Cells[coordinate.X, coordinate.Y]; }
            set { Cells[coordinate.X, coordinate.Y] = value; }
        }
    }
}