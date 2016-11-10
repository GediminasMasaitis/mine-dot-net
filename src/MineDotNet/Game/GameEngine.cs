using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.Game
{
    public class GameEngine
    {
        public GameMapGenerator Generator { get; set; }
        public GameMap GameMap { get; set; }

        public GameEngine(GameMapGenerator generator)
        {
            Generator = generator;
        }

        public void StartNew(int width, int height, Coordinate startingPosition, double mineDensity)
        {
            GameMap = Generator.GenerateWithMineDensity(width, height, startingPosition, mineDensity);
        }

        public void StartNew(int width, int height, Coordinate startingPosition, int mineCount)
        {
            GameMap = Generator.GenerateWithMineCount(width, height, startingPosition, mineCount);
        }

        public void SetFlag(Coordinate coordinate, CellFlag flag)
        {
            var cell = GameMap[coordinate];
            if (GameMap.RemainingMineCount.HasValue)
            {
                if (cell.Flag != CellFlag.HasMine && flag == CellFlag.HasMine)
                {
                    GameMap.RemainingMineCount--;
                }
                else if(cell.Flag == CellFlag.HasMine && flag != CellFlag.HasMine)
                {
                    GameMap.RemainingMineCount++;
                }
            }
            cell.Flag = flag;

        }

        public bool OpenCell(Coordinate coordinate)
        {
            var initialCell = GameMap[coordinate];
            if (initialCell.HasMine)
            {
                return false;
            }

            var toOpen = new HashSet<Coordinate>();
            toOpen.Add(coordinate);
            while (toOpen.Count > 0)
            {
                var coord = toOpen.First();
                var cell = GameMap[coord];
                cell.State = CellState.Empty;
                toOpen.Remove(coord);
                if (cell.Hint == 0)
                {
                    var neighboursToOpen = GameMap.CalculateNeighboursOf(cell.Coordinate).Where(x => x.State == CellState.Filled).Select(x => x.Coordinate);
                    toOpen.UnionWith(neighboursToOpen);
                }
            }
            return true;
        }
    }
}
