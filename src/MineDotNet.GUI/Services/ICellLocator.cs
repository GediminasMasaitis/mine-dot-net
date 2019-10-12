using System.Drawing;
using MineDotNet.Common;

namespace MineDotNet.GUI.Services
{
    internal interface ICellLocator
    {
        Size GetCellSize(IMap map, Size canvasSize);
        Coordinate GetCellCoordinate(Point location, IMap map, Size canvasSize);
        Coordinate GetCellCoordinate(Point location, Size cellSize);
    }
}