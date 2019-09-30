using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MineDotNet.GUI.Tiles
{
    class TileGenerator : ITileGenerator
    {
        public Image GenerateTile(Size size)
        {
            // TODO: Implement tile generation
            var image = new Bitmap(size.Width, size.Height);
            return image;
        }
    }
}
