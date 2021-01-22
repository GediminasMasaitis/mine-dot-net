using MineDotNet.Common;

namespace MineDotNet.Game.Models
{
    public class GameCell : Cell
    {
        public bool HasMine { get; set; }

        public GameCell(int x, int y, bool hasMine, CellState state = CellState.Empty, CellFlag flag = CellFlag.None, int hint = 0) : this(new Coordinate(x,y), hasMine, state, flag, hint)
        {
        }

        public GameCell(Coordinate coord, bool hasMine, CellState state = CellState.Empty, CellFlag flag = CellFlag.None, int hint = 0) : base(coord, state, flag, hint)
        {
            HasMine = hasMine;
        }
    }
}