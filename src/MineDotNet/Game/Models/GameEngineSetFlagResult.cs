using MineDotNet.Common;

namespace MineDotNet.Game.Models
{
    public class GameEngineSetFlagResult
    {
        public Coordinate Coordinate { get; }
        public CellFlag Flag { get; }
        public bool FlagCorrect { get; }

        public GameEngineSetFlagResult(Coordinate coordinate, CellFlag flag, bool flagCorrect)
        {
            Coordinate = coordinate;
            Flag = flag;
            FlagCorrect = flagCorrect;
        }
    }
}