using MineDotNet.Common;
using MineDotNet.Game.Models;

namespace MineDotNet.Game
{
    public interface IGameEngine
    {
        GameEngineSetFlagResult ToggleFlag(GameMap gameMap, Coordinate coordinate);
        GameEngineSetFlagResult SetFlag(GameMap gameMap, Coordinate coordinate, CellFlag flag);
        GameEngineOpenCellResult OpenCell(GameMap gameMap, Coordinate coordinate);
    }
}