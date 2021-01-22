using System;
using System.Collections.Generic;
using System.Linq;
using MineDotNet.Common;
using MineDotNet.Game;
using MineDotNet.Game.Models;

namespace MineDotNet.ML
{
    public class TrainMapGenerator
    {
        private readonly Random _random;
        private readonly GameManager _manager;

        public TrainMapGenerator(Random random, GameManager manager)
        {
            _random = random;
            _manager = manager;
        }

        public IEnumerable<GameMap> CreateMaps()
        {
            const int sliceSize = 5;
            const int mapSize = 12;

            while (true)
            {
                var x = _random.Next(0, mapSize - sliceSize);
                var y = _random.Next(0, mapSize - sliceSize);

                _manager.StartWithMineDensity(mapSize, mapSize, new Coordinate(mapSize / 2, mapSize / 2), true, 0.2);
                var gameMap = _manager.CurrentMap;
                
                var slice = Slice(gameMap, new Coordinate(x, y), sliceSize, sliceSize);
                var emptyCount = slice.AllCells.Count(c => c.State == CellState.Empty);
                if (emptyCount < 5 || emptyCount > 15)
                {
                    continue;
                }

                if (slice[new Coordinate(2, 2)].State != CellState.Filled)
                {
                    continue;
                }
                
                yield return slice;
            }
        }

        public GameMap Slice(GameMap baseMap, Coordinate start, int width, int height)
        {
            var slice = new GameMap(width, height, null, null, true, false);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    var from = new Coordinate(start.X + i, start.Y + j);
                    var to = new Coordinate(i, j);
                    var baseCell = baseMap[from];
                    var cell = new GameCell(to, baseCell.HasMine, baseCell.State, baseCell.Flag, baseCell.Hint);
                    slice[to] = cell;
                }
            }
            return slice;
        }
    }
}
