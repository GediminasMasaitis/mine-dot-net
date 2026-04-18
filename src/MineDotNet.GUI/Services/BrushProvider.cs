using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MineDotNet.GUI.Services
{
    internal class BrushProvider : IBrushProvider
    {
        public IReadOnlyList<SolidBrush> Brushes { get; }
        public IReadOnlyList<Color> LabelColors { get; }
        public SolidBrush EmptyBrush { get; }

        public BrushProvider()
        {
            // Alpha 110 is just visible enough over the dark board background without
            // washing out the tile graphics underneath. Label colours use the same
            // hues at full alpha + a brightness boost so the mask text stays readable.
            var baseColors = new List<Color>
            {
                Color.FromArgb(210, 90, 90),    // red
                Color.FromArgb(110, 200, 120),  // green
                Color.FromArgb(100, 160, 240),  // blue
                Color.FromArgb(220, 200, 100),  // yellow
                Color.FromArgb(200, 120, 200),  // magenta
                Color.FromArgb(100, 200, 200),  // cyan
                Color.FromArgb(230, 150, 80),   // orange
                Color.FromArgb(120, 220, 170),  // mint
                Color.FromArgb(180, 140, 100),  // brown
                Color.FromArgb(230, 120, 180),  // pink
                Color.FromArgb(220, 190, 120),  // gold
                Color.FromArgb(170, 170, 170),  // light gray
                Color.FromArgb(140, 160, 220),  // lavender
            };

            var rng = new Random(0);
            while (baseColors.Count < 32)
            {
                var r = rng.Next(120, 240);
                var g = rng.Next(120, 240);
                var b = rng.Next(120, 240);
                baseColors.Add(Color.FromArgb(r, g, b));
            }

            Brushes = baseColors
                .Select(c => new SolidBrush(Color.FromArgb(110, c.R, c.G, c.B)))
                .ToList();
            LabelColors = baseColors;

            EmptyBrush = new SolidBrush(Color.FromArgb(40, 40, 44));
        }
    }
}
