using System.Reflection;
using MineDotNet.Common;
using MineDotNet.Game.Models;

namespace MineDotNet.Game
{
    public class GameMapGenerationParameters
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public int? MineCount { get; set; }
        public double? MineDensity { get; set; }

        public bool GuaranteeOpening { get; set; }
        public Coordinate StartingPosition { get; set; }
    }

    public class GameManager : IGameManager
    {
        private readonly IGameMapGenerator _generator;
        private readonly IGameEngine _engine;

        public GameMap CurrentMap { get; private set; }
        public bool GameStarted => CurrentMap != null;

        public bool MinesPositioned { get; private set; }

        private GameMapGenerationParameters _currentParameters;

        public GameManager(IGameMapGenerator generator, IGameEngine engine)
        {
            _generator = generator;
            _engine = engine;
        }

        public void Start()
        {
            
        }

        public void Start(GameMap gameMap)
        {
            CurrentMap = gameMap;
            if (CurrentMap.StartingPosition != null)
            {
                _engine.OpenCell(CurrentMap, CurrentMap.StartingPosition);
            }
        }

        public void StartWithMineDensity(int width, int height, Coordinate startingPosition, bool guaranteeOpening, double mineDensity)
        {
            var map = _generator.GenerateWithMineDensity(width, height, startingPosition, guaranteeOpening, mineDensity);
            Start(map);
        }

        public void StartWithMineCount(int width, int height, Coordinate startingPosition, bool guaranteeOpening, int mineCount)
        {
            var map = _generator.GenerateWithMineCount(width, height, startingPosition, guaranteeOpening, mineCount);
            Start(map);
        }

        public void Stop()
        {
            CurrentMap = null;
        }

        public GameEngineOpenCellResult OpenCell(Coordinate coordinate) => _engine.OpenCell(CurrentMap, coordinate);
        public GameEngineSetFlagResult ToggleFlag(Coordinate coordinate) => _engine.ToggleFlag(CurrentMap, coordinate);
        public GameEngineSetFlagResult SetFlag(Coordinate coordinate, CellFlag flag) => _engine.SetFlag(CurrentMap, coordinate, flag);
    }
}