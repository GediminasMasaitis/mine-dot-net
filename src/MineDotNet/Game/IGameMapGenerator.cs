using System.Collections.Generic;
using MineDotNet.Common;
using MineDotNet.Game.Models;

namespace MineDotNet.Game
{
    public interface IGameMapGenerator
    {
        IEnumerable<GameMap> GenerateSequenceWithMineDensity(int width, int height, Coordinate startingPosition, bool guaranteeOpening, double mineDensity);
        GameMap GenerateWithMineDensity(int width, int height, Coordinate startingPosition, bool guaranteeOpening, double mineDensity);
        IEnumerable<GameMap> GenerateSequenceWithMineCount(int width, int height, Coordinate startingPosition, bool guaranteeOpening, int mineCount);
        GameMap GenerateWithMineCount(int width, int height, Coordinate startingPosition, bool guaranteeOpening, int mineCount);
    }
}