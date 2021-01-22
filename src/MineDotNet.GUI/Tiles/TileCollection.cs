using System.Collections.Generic;
using System.Drawing;
using MineDotNet.Common;

namespace MineDotNet.GUI.Tiles
{
    class TileCollection
    {
        public IDictionary<int, Image> Hints { get; }
        public IDictionary<CellState, Image> States { get; }
        public IDictionary<CellFlag, Image> Flags { get; }
        public Image UnrevealedMine { get; set; }

        public TileCollection()
        {
            Hints = new Dictionary<int, Image>();
            States = new Dictionary<CellState, Image>();
            Flags = new Dictionary<CellFlag, Image>();
        }
    }
}