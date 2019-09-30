using System.Drawing;

namespace MineDotNet.GUI.Tiles
{
    internal class TileProvider : ITileProvider
    {
        private readonly ITileLoader _loader;
        private readonly ITileResizer _resizer;
        private readonly ITileGenerator _generator;

        private bool _tilesLoaded;

        public TileProvider(ITileLoader loader, ITileResizer resizer, ITileGenerator tileGenerator)
        {
            _loader = loader;
            _resizer = resizer;
            _generator = tileGenerator;

            _tilesLoaded = false;
        }

        public TileCollection GetTiles(Size size)
        {
            var originalTiles = _loader.GetTiles();
            var resizedTiles = _resizer.ReszizeTiles(originalTiles, size);
            return resizedTiles;
        }

        public TileCollection GetTiles(int width, int height)
        {
            var size = new Size(width, height);
            return GetTiles(size);
        }
    }
}