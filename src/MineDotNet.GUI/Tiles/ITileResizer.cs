using System.Drawing;

namespace MineDotNet.GUI.Tiles
{
    internal interface ITileResizer
    {
        TileCollection ReszizeTiles(TileCollection originalTiles, Size size);
        TileCollection ReszizeTiles(TileCollection originalTiles, int width, int height);
    }
}