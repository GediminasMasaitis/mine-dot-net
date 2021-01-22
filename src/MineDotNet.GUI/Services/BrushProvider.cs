using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MineDotNet.GUI.Services
{
    internal class BrushProvider : IBrushProvider
    {
        public IReadOnlyList<SolidBrush> Brushes { get; }
        public SolidBrush EmptyBrush { get; }

        public BrushProvider()
        {
            Brushes = CreateBrushes(10);
            EmptyBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
        }

        private IReadOnlyList<SolidBrush> CreateBrushes(int colorCount)
        {
            var colors = new List<Color>
            {
                //Color.FromArgb(0, 0, 0),
                Color.FromArgb(100, 150, 0, 0),
                Color.FromArgb(100, 0, 150, 0),
                Color.FromArgb(100, 40, 70, 220),
                Color.FromArgb(100,100, 100, 0),
                Color.FromArgb(100,100, 0, 100),
                Color.FromArgb(100,0, 100, 100),
                Color.FromArgb(100,170, 70, 0),
                Color.FromArgb(100,0, 170, 100),
                Color.FromArgb(100,70, 30, 0),
                Color.FromArgb(100,180, 0, 100),
                Color.FromArgb(100,180, 150, 50),
                Color.FromArgb(100,120, 120, 120),
                Color.FromArgb(100,170, 170, 170),
            };

            var rng = new Random(0);
            while (colors.Count < colorCount)
            {
                var red = rng.Next(0, 256);
                var green = rng.Next(0, 256);
                var blue = rng.Next(0, 256);
                var color = Color.FromArgb(red, green, blue);
                colors.Add(color);
            }

            var brushes = colors.Select(x => new SolidBrush(x)).ToList();
            return brushes;
        }
    }
}
