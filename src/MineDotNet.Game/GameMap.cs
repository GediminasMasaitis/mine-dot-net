using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.Game
{
    class GameMap
    {
        public GameCell[,] Cells { get; private set; }

        public GameCell this[Coordinate coordinate]
        {
            get { return Cells[coordinate.X, coordinate.Y]; }
            set { Cells[coordinate.X, coordinate.Y] = value; }
        }
    }
}
