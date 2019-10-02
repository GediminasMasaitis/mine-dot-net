using MineDotNet.Common;

namespace MineDotNet.GUI.Models
{
    class Mask
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool[,] Cells { get; set; }

        public Mask(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new bool[width, height];
        }


    }
}
