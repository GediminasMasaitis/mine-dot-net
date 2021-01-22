using System.Drawing;
using MineDotNet.Common;

namespace MineDotNet.GUI.Services
{
    internal interface ICellLocator
    {
        Size GetCellSize(IReadOnlyMapBase<Cell> map, Size canvasSize);
        Coordinate GetCellCoordinate(Point location, IReadOnlyMapBase<Cell> map, Size canvasSize);
        Coordinate GetCellCoordinate(Point location, Size cellSize);
    }
}