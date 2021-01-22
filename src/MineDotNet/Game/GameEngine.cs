using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MineDotNet.Common;
using MineDotNet.Game.Models;

namespace MineDotNet.Game
{
    public class GameEngine : IGameEngine
    {
        public GameEngineOpenCellResult OpenCell(GameMap gameMap, Coordinate coordinate)
        {
            var cell = gameMap[coordinate];
            if (cell.HasMine)
            {
                cell.State = CellState.Mine;
                return new GameEngineOpenCellResult(coordinate, false);
            }

            CascadeOpen(gameMap, coordinate);
            return new GameEngineOpenCellResult(coordinate, true);
        }

        private void CascadeOpen(GameMap gameMap, Coordinate coordinate)
        {
            var toOpen = new HashSet<Coordinate>();
            toOpen.Add(coordinate);
            while (toOpen.Count > 0)
            {
                var coord = toOpen.First();
                var cell = gameMap[coord];
                cell.State = CellState.Empty;
                toOpen.Remove(coord);
                if (cell.Hint == 0)
                {
                    var neighboursToOpen = gameMap.CalculateNeighboursOf(cell.Coordinate).Where(x => x.State == CellState.Filled).Select(x => x.Coordinate);
                    toOpen.UnionWith(neighboursToOpen);
                }
            }
        }
        
        public GameEngineSetFlagResult ToggleFlag(GameMap gameMap, Coordinate coordinate)
        {
            var cell = gameMap[coordinate];
            switch (cell.Flag)
            {
                case CellFlag.None:
                case CellFlag.NotSure:
                    return SetFlag(gameMap, coordinate, CellFlag.HasMine);
                case CellFlag.HasMine:
                    return SetFlag(gameMap, coordinate, CellFlag.None);
                default:
                    throw new ArgumentOutOfRangeException(nameof(cell.Flag), cell.Flag, null);
            }
        }

        public GameEngineSetFlagResult SetFlag(GameMap gameMap, Coordinate coordinate, CellFlag flag)
        {
            var cell = gameMap[coordinate];
            if (cell.Flag != CellFlag.HasMine && flag == CellFlag.HasMine)
            {
                gameMap.RemainingMineCount--;
            }
            else if (cell.Flag == CellFlag.HasMine && flag != CellFlag.HasMine)
            {
                gameMap.RemainingMineCount++;
            }
            cell.Flag = flag;

            var correct = cell.HasMine ? flag != CellFlag.DoesntHaveMine : flag != CellFlag.HasMine;
            var result = new GameEngineSetFlagResult(coordinate, flag, correct);
            return result;
        }
    }
}
