using MineDotNet.Common;

namespace MineDotNet.Game.Models
{
    public class GameEngineOpenCellResult
    {
        public Coordinate Coordinate { get; }
        public bool OpenCorrect { get; }

        public GameEngineOpenCellResult(Coordinate coordinate, bool openCorrect)
        {
            Coordinate = coordinate;
            OpenCorrect = openCorrect;
        }
    }
}