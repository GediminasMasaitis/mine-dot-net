using System.Collections.Generic;
using System.Drawing;

namespace MineDotNet.GUI.Tiles
{
    internal class TileProvider : ITileProvider
    {
        private readonly ITileLoader _loader;
        private readonly ITileResizer _resizer;
        private readonly ITileGenerator _generator;

        private TileCollection _originalTiles;
        private readonly IDictionary<Size, TileCollection> _tileCache;

        public TileProvider(ITileLoader loader, ITileResizer resizer, ITileGenerator tileGenerator)
        {
            _loader = loader;
            _resizer = resizer;
            _generator = tileGenerator;

            _tileCache = new Dictionary<Size, TileCollection>();
        }

        public TileCollection GetTiles(Size size)
        {
            if (_originalTiles == null)
            {
                _originalTiles = _loader.GetTiles();
            }

            if (!_tileCache.TryGetValue(size, out var tiles))
            {
                tiles = _resizer.ReszizeTiles(_originalTiles, size);
                _tileCache.Add(size, tiles);
            }

            return tiles;
        }

        public TileCollection GetTiles(int width, int height)
        {
            var size = new Size(width, height);
            return GetTiles(size);
        }
    }
}