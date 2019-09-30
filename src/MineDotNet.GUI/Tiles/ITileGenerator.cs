using System.Drawing;

namespace MineDotNet.GUI.Tiles
{
    internal interface ITileGenerator
    {
        Image GenerateTile(Size size);
    }
}