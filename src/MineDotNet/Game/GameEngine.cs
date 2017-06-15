using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.Game
{
    public class GameEngine
    {
        public bool FailOnIncorrectFlag { get; set; }
        public GameMap GameMap { get; set; }
        public bool GameStarted => GameMap != null;

        public void Start(GameMap gameMap)
        {
            GameMap = gameMap;
            if (GameMap.StartingPosition != null)
            {
                OpenCell(GameMap.StartingPosition);
            }
        }

        public void StartWithMineDensity(GameMapGenerator generator, int width, int height, Coordinate startingPosition, bool guaranteeOpening, double mineDensity)
        {
            GameMap = generator.GenerateWithMineDensity(width, height, startingPosition, guaranteeOpening, mineDensity);
            if (startingPosition != null)
            {
                OpenCell(startingPosition);
            }
        }

        public void StartWithMineCount(GameMapGenerator generator, int width, int height, Coordinate startingPosition, bool guaranteeOpening, int mineCount)
        {
            GameMap = generator.GenerateWithMineCount(width, height, startingPosition, guaranteeOpening, mineCount);
            if (startingPosition != null)
            {
                OpenCell(startingPosition);
            }
        }

        public bool SetFlag(Coordinate coordinate, CellFlag flag)
        {
            var cell = GameMap[coordinate];
            var oldFlag = cell.Flag;
            cell.Flag = flag;
            if (oldFlag != CellFlag.HasMine && flag == CellFlag.HasMine)
            {
                GameMap.RemainingMineCount--;
                if (FailOnIncorrectFlag && !cell.HasMine)
                {
                    return false;
                }
            }
            else if(oldFlag == CellFlag.HasMine && flag != CellFlag.HasMine)
            {
                GameMap.RemainingMineCount++;
            }
            return true;
        }

        public bool ToggleFlag(Coordinate coordinate)
        {
            var cell = GameMap[coordinate];
            switch (cell.Flag)
            {
                case CellFlag.None:
                    return SetFlag(coordinate, CellFlag.HasMine);
                case CellFlag.HasMine:
                    return SetFlag(coordinate, CellFlag.None);
                case CellFlag.NotSure:
                    return SetFlag(coordinate, CellFlag.HasMine);
                default:
                    throw new ArgumentOutOfRangeException(nameof(cell.Flag), cell.Flag, null);
            }
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
