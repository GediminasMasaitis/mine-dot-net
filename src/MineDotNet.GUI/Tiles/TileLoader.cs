using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using MineDotNet.Common;

namespace MineDotNet.GUI.Tiles
{
    internal class TileLoader : ITileLoader
    {
        private void LoadTileIfExists<TKey>(IDictionary<TKey, Image> dictionary, TKey key, string path, string tileName)
        {
            var tilePath = Path.Combine(path, tileName);
            if (File.Exists(tilePath))
            {
                var tile = LoadImage(tilePath);
                dictionary.Add(key, tile);
            }
        }

        private Image LoadImage(string tilePath)
        {
            return Image.FromFile(tilePath);
        }

        private void LoadHints(TileCollection tiles, string path)
        {
            tiles.Hints.Clear();
            for (var i = 0; i <= 8; i++)
            {
                var hintName = $"{i}.png";
                LoadTileIfExists(tiles.Hints, i, path, hintName);
            }
        }

        private void LoadFlags(TileCollection tiles, string path)
        {
            tiles.Flags.Clear();
            LoadTileIfExists(tiles.Flags, CellFlag.HasMine, path, "flag.png");
            LoadTileIfExists(tiles.Flags, CellFlag.DoesntHaveMine, path, "antiflag.png");
            LoadTileIfExists(tiles.Flags, CellFlag.NotSure, path, "unknown.png");
        }

        private void LoadStates(TileCollection tiles, string path)
        {
            tiles.States.Clear();
            if (tiles.States.TryGetValue(0, out var emptyTile))
            {
                tiles.States.Add(CellState.Empty, emptyTile);
            }

            LoadTileIfExists(tiles.States, CellState.Filled, path, "filled.png");
            LoadTileIfExists(tiles.States, CellState.Wall, path, "wall.png");
            LoadTileIfExists(tiles.States, CellState.Mine, path, "mine.png");
        }

        private TileCollection GetTiles(string path)
        {
            var tiles = new TileCollection(); 
            if (!Directory.Exists(path))
            {
                return tiles;
            }

            LoadHints(tiles, path);
            LoadFlags(tiles, path);
            LoadStates(tiles, path);
            LoadUnrevealed(tiles, path);
            return tiles;
        }

        private void LoadUnrevealed(TileCollection tiles, string path)
        {
            var tilePath = Path.Combine(path, "mine_hidden.png");
            tiles.UnrevealedMine = LoadImage(tilePath);
        }

        public TileCollection GetTiles()
        {
            var assmeblyLocation = Assembly.GetExecutingAssembly().Location;
            var currentPath = Path.GetDirectoryName(assmeblyLocation);
            var path = Path.Combine(currentPath, "assets");
            return GetTiles(path);
        }
    }
}
