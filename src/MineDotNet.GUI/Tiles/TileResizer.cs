using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MineDotNet.GUI.Tiles
{
    class TileResizer : ITileResizer
    {
        private Image ResizeImage(Image image, Size size)
        {
            var rect = new Rectangle(Point.Empty, size);
            var bitmap = new Bitmap(size.Width, size.Height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = InterpolationMode.High;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var attr = new ImageAttributes())
                {
                    attr.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
                }
            }
            return bitmap;
        }

        private void ResizeToDictionary<TKey>(IDictionary<TKey, Image> source, IDictionary<TKey, Image> target, Size size)
        {
            foreach (var pair in source)
            {
                var resizedTile = ResizeImage(pair.Value, size);
                target.Add(pair.Key, resizedTile);
            }
        }

        public TileCollection ReszizeTiles(TileCollection originalTiles, Size size)
        {
            var tiles = new TileCollection();
            ResizeToDictionary(originalTiles.Hints, tiles.Hints, size);
            ResizeToDictionary(originalTiles.Flags, tiles.Flags, size);
            ResizeToDictionary(originalTiles.States, tiles.States, size);
            tiles.UnrevealedMine = ResizeImage(originalTiles.UnrevealedMine, size);
            return tiles;
        }

        public TileCollection ReszizeTiles(TileCollection originalTiles, int width, int height)
        {
            var size = new Size(width, height);
            return ReszizeTiles(originalTiles, size);
        }
    }
}