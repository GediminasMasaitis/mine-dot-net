using System;
using System.Collections.Generic;
using System.Linq;
using MineDotNet.Common;

namespace MineDotNet.Game
{
    public class GameMapGenerator
    {
        public Random Random { get; set; }

        public GameMapGenerator(Random random = null)
        {
            Random = random ?? new Random();
        }

        public GameMap GenerateWithMineDensity(int width, int height, Coordinate startingPosition, double mineDensity)
        {
            if (mineDensity < 0 || mineDensity > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(mineDensity));
            }
            var area = width*height;
            var count = (int)(area*mineDensity);
            var map = GenerateWithMineCount(width, height, startingPosition, count);
            return map;
        }

        public GameMap GenerateWithMineCount(int width, int height, Coordinate startingPosition, int mineCount)
        {
            var coordinates = new List<Coordinate>();
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    coordinates.Add(new Coordinate(i,j));
                }
            }


            var map = new GameMap(width, height, mineCount, true, CellState.Filled);

            if (startingPosition != null)
            {
                coordinates.Remove(startingPosition);
                map[startingPosition].State = CellState.Empty;
            }
            Shuffle(coordinates);

            var mineCoordinates = coordinates.Take(mineCount);
            foreach (var mineCoordinate in mineCoordinates)
            {
                map[mineCoordinate].HasMine = true;
            }

            foreach (var cell in map.Cells)
            {
                if (cell.HasMine)
                {
                    continue;
                }
                var cellNeighbours = map.CalculateNeighboursOf(cell.Coordinate);
                cell.Hint = cellNeighbours.Count(x => x.HasMine);
            }
            return map;
        }

        private void Shuffle<T>(IList<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                n--;
                var k = Random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}