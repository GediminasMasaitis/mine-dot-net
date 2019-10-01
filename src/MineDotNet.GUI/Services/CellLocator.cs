using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MineDotNet.Common;

namespace MineDotNet.GUI.Services
{
    internal interface ICellLocator
    {
        Size GetCellSize(IMap map, Size canvasSize);
        Coordinate GetCellCoordinate(Point location, IMap map, Size canvasSize);
        Coordinate GetCellCoordinate(Point location, Size cellSize);
    }

    class CellLocator : ICellLocator
    {
        public Size GetCellSize(IMap map, Size canvasSize)
        {
            var cellWidth = canvasSize.Width / map.Height;
            var cellHeight = canvasSize.Height / map.Width;
            if (cellHeight > cellWidth)
            {
                cellHeight = cellWidth;
            }
            if (cellWidth > cellHeight)
            {
                cellWidth = cellHeight;
            }

            return new Size(cellWidth, cellHeight);
        }

        public Coordinate GetCellCoordinate(Point location, IMap map, Size canvasSize)
        {
            var cellSize = GetCellSize(map, canvasSize);
            return GetCellCoordinate(location, cellSize);
        }

        public Coordinate GetCellCoordinate(Point location, Size cellSize)
        {
            var x = location.Y / cellSize.Width;
            var y = location.X / cellSize.Height;
            var coordinate = new Coordinate(x, y);
            return coordinate;
        }
    }
}