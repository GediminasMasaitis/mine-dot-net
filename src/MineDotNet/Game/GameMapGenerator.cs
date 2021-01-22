using System;
using System.Collections.Generic;
using System.Linq;
using MineDotNet.Common;
using MineDotNet.Game.Models;

namespace MineDotNet.Game
{
    public class GameMapGenerator : IGameMapGenerator
    {
        private readonly Random _random;

        public GameMapGenerator(Random random = null)
        {
            _random = random ?? new Random();
        }

        public IEnumerable<GameMap> GenerateSequenceWithMineDensity(int width, int height, Coordinate startingPosition, bool guaranteeOpening, double mineDensity)
        {
            while (true)
            {
                yield return GenerateWithMineDensity(width, height, startingPosition, guaranteeOpening, mineDensity);
            }
        }

        public GameMap GenerateWithMineDensity(int width, int height, Coordinate startingPosition, bool guaranteeOpening, double mineDensity)
        {
            if (mineDensity < 0 || mineDensity > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(mineDensity));
            }
            var area = width*height;
            var count = (int)(area*mineDensity);
            var map = GenerateWithMineCount(width, height, startingPosition, guaranteeOpening, count);
            return map;
        }

        public IEnumerable<GameMap> GenerateSequenceWithMineCount(int width, int height, Coordinate startingPosition, bool guaranteeOpening, int mineCount)
        {
            while (true)
            {
                yield return GenerateWithMineCount(width, height, startingPosition, guaranteeOpening, mineCount);
            }
        }

        public GameMap GenerateWithMineCount(int width, int height, Coordinate startingPosition, bool guaranteeOpening, int mineCount)
        {
            var coordinates = new List<Coordinate>();
            for (var i = 0; i < width; i++)
            {
                for (var j = 0; j < height; j++)
                {
                    coordinates.Add(new Coordinate(i,j));
                }
            }


            var map = new GameMap(width, height, mineCount, startingPosition, guaranteeOpening, true, CellState.Filled);

            if (startingPosition != null)
            {
                coordinates.Remove(startingPosition);
                if (guaranteeOpening)
                {
                    var startingNeighbours = map.CalculateNeighboursOf(startingPosition);
                    foreach (var startingNeighbour in startingNeighbours)
                    {
                        coordinates.Remove(startingNeighbour.Coordinate);
                    }
                }
                //map[startingPosition].State = CellState.Empty;
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
                var k = _random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}