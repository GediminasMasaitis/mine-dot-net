namespace MineDotNet.Common
{
    public class Cell
    {
        public Cell(int x, int y, CellState state = CellState.Empty, CellFlag flag = CellFlag.None, int hint = 0) : this(new Coordinate(x,y), state, flag, hint)
        {
        }

        public Cell(Coordinate coord, CellState state = CellState.Empty, CellFlag flag = CellFlag.None, int hint = 0)
        {
            Coordinate = coord;
            State = state;
            Flag = flag;
            Hint = hint;
        }

        public Coordinate Coordinate { get; }

        public int X => Coordinate.X;
        public int Y => Coordinate.Y;
        public CellState State { get; set; }
        public CellFlag Flag { get; set; }
        public int Hint { get; set; }

        public override string ToString()
        {
            return Coordinate.ToString() + ": " + State + ", " + Flag + ", " + Hint;
        }
    }
}