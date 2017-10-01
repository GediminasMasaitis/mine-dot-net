using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MineDotNet.Common;

namespace MineDotNet.GUI
{
    class MaskMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool[,] Cells { get; set; }

        public MaskMap(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new bool[width, height];
        }

        public static MaskMap FromMap(Map map)
        {
            var maskMap = new MaskMap(map.Width, map.Height);
            foreach(var cell in map.AllCells)
            {
                maskMap.Cells[cell.X, cell.Y] = cell.State == CellState.Filled;
            }
            return maskMap;
        }
    }
}
