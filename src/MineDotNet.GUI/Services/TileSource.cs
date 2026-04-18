using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MineDotNet.Common;

namespace MineDotNet.GUI.Services
{
    internal sealed class TileSource : ITileSource
    {
        public IReadOnlyDictionary<int, ImageSource> Hints { get; }
        public IReadOnlyDictionary<CellState, ImageSource> States { get; }
        public IReadOnlyDictionary<CellFlag, ImageSource> Flags { get; }
        public ImageSource UnrevealedMine { get; }

        public TileSource()
        {
            var assetsDir = ResolveAssetsDir();

            var hints = new Dictionary<int, ImageSource>();
            for (var i = 0; i <= 8; i++)
            {
                TryLoad(assetsDir, $"{i}.png", img => hints[i] = img);
            }
            Hints = hints;

            var states = new Dictionary<CellState, ImageSource>();
            TryLoad(assetsDir, "filled.png", img => states[CellState.Filled] = img);
            TryLoad(assetsDir, "wall.png", img => states[CellState.Wall] = img);
            TryLoad(assetsDir, "mine.png", img => states[CellState.Mine] = img);
            if (hints.TryGetValue(0, out var empty)) states[CellState.Empty] = empty;
            States = states;

            var flags = new Dictionary<CellFlag, ImageSource>();
            TryLoad(assetsDir, "flag.png", img => flags[CellFlag.HasMine] = img);
            TryLoad(assetsDir, "antiflag.png", img => flags[CellFlag.DoesntHaveMine] = img);
            TryLoad(assetsDir, "unknown.png", img => flags[CellFlag.NotSure] = img);
            Flags = flags;

            ImageSource hidden = null;
            TryLoad(assetsDir, "mine_hidden.png", img => hidden = img);
            UnrevealedMine = hidden;
        }

        private static void TryLoad(string dir, string fileName, Action<ImageSource> assign)
        {
            var path = Path.Combine(dir, fileName);
            if (!File.Exists(path)) return;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            assign(bmp);
        }

        private static string ResolveAssetsDir()
        {
            var exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(exeDir ?? string.Empty, "assets");
        }
    }
}
