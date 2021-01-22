using MineDotNet.Common;
using MineDotNet.Game.Models;

namespace MineDotNet.Game
{
    public interface IGameManager
    {
        GameMap CurrentMap { get; }
        bool GameStarted { get; }
        void Start(GameMap gameMap);
        void StartWithMineDensity(int width, int height, Coordinate startingPosition, bool guaranteeOpening, double mineDensity);
        void StartWithMineCount(int width, int height, Coordinate startingPosition, bool guaranteeOpening, int mineCount);
        void Stop();
        GameEngineOpenCellResult OpenCell(Coordinate coordinate);
        GameEngineSetFlagResult ToggleFlag(Coordinate coordinate);
        GameEngineSetFlagResult SetFlag(Coordinate coordinate, CellFlag flag);
    }
}