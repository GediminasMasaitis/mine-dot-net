using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace MineDotNet.GUI.Services
{
    internal sealed class PaletteProvider : IPaletteProvider
    {
        public IReadOnlyList<Color> MaskColors { get; }
        public IReadOnlyList<Brush> MaskOverlayBrushes { get; }

        public PaletteProvider()
        {
            // Colours chosen for readability against the Fluent dark surface. Alpha ~110
            // on the overlay brushes is just enough to tint a revealed cell without
            // obliterating the tile artwork underneath.
            var baseColors = new List<Color>
            {
                Color.FromRgb(230, 100, 100), // red
                Color.FromRgb(110, 205, 130), // green
                Color.FromRgb(95, 170, 245),  // blue
                Color.FromRgb(225, 205, 110), // yellow
                Color.FromRgb(205, 130, 210), // magenta
                Color.FromRgb(100, 210, 210), // cyan
                Color.FromRgb(235, 155, 85),  // orange
                Color.FromRgb(125, 225, 175), // mint
                Color.FromRgb(190, 150, 110), // brown
                Color.FromRgb(235, 130, 190), // pink
                Color.FromRgb(225, 195, 125), // gold
                Color.FromRgb(180, 180, 180), // light gray
                Color.FromRgb(150, 170, 225), // lavender
            };

            var rng = new Random(0);
            while (baseColors.Count < 32)
            {
                baseColors.Add(Color.FromRgb(
                    (byte)rng.Next(130, 240),
                    (byte)rng.Next(130, 240),
                    (byte)rng.Next(130, 240)));
            }

            MaskColors = baseColors;
            MaskOverlayBrushes = baseColors
                .Select(c =>
                {
                    var b = new SolidColorBrush(Color.FromArgb(110, c.R, c.G, c.B));
                    b.Freeze();
                    return (Brush)b;
                })
                .ToList();
        }
    }
}
