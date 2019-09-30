using System.Drawing;

namespace MineDotNet.GUI.Tiles
{
    internal interface ITileProvider
    {
        TileCollection GetTiles(Size size);
        TileCollection GetTiles(int width, int height);
    }
}