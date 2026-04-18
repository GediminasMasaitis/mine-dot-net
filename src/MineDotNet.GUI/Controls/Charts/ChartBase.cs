using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Hand-drawn chart primitives. Every subclass overrides OnRender and
    // calls ChartBase helpers for axes/labels so the three charts share one
    // visual language (same axis colour, same label font, same padding).
    internal abstract class ChartBase : FrameworkElement
    {
        protected static readonly Brush LabelBrush = Frozen(Color.FromRgb(180, 180, 190));
        protected static readonly Brush SubtleBrush = Frozen(Color.FromRgb(130, 130, 140));
        protected static readonly Pen AxisPen = FrozenPen(Color.FromRgb(110, 110, 120), 1);
        protected static readonly Pen GridPen = FrozenPen(Color.FromArgb(50, 150, 150, 160), 1);
        private static readonly FontFamily LabelFamily = new FontFamily("Segoe UI");
        protected static readonly Typeface LabelFace = new Typeface(LabelFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        protected static readonly Typeface TitleFace = new Typeface(LabelFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        protected IReadOnlyList<BenchmarkSolverRun> Runs = Array.Empty<BenchmarkSolverRun>();
        protected IReadOnlyList<Color> SolverColors = Array.Empty<Color>();

        protected ChartBase()
        {
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            MinWidth = 120;
            MinHeight = 120;
        }

        public void SetRuns(IReadOnlyList<BenchmarkSolverRun> runs, IReadOnlyList<Color> colors)
        {
            Runs = runs ?? Array.Empty<BenchmarkSolverRun>();
            SolverColors = colors ?? Array.Empty<Color>();
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var w = double.IsInfinity(availableSize.Width) ? MinWidth : availableSize.Width;
            var h = double.IsInfinity(availableSize.Height) ? MinHeight : availableSize.Height;
            return new Size(w, h);
        }

        protected FormattedText Label(string s, double size = 10.5, Brush brush = null, bool title = false)
            => new FormattedText(
                s,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                title ? TitleFace : LabelFace,
                size,
                brush ?? LabelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

        // Rounds v up to the next "nice" axis max (1, 2, 5, 10, 20, 50, 100, ...).
        // Keeps tick labels readable instead of 873 ms etc.
        protected static double NiceCeiling(double v)
        {
            if (v <= 0) return 1;
            var mag = Math.Pow(10, Math.Floor(Math.Log10(v)));
            var norm = v / mag;
            var nice = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
            return nice * mag;
        }

        protected Brush SolverBrush(int i)
        {
            var c = i < SolverColors.Count ? SolverColors[i] : Colors.White;
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        protected Pen SolverPen(int i, double thickness = 2)
        {
            var p = new Pen(SolverBrush(i), thickness);
            p.Freeze();
            return p;
        }

        // Legend at the given top-left anchor, one swatch + name per solver.
        // Returns the used vertical extent so callers can avoid overlap.
        protected double DrawLegend(DrawingContext dc, double x, double y, double maxWidth)
        {
            const double swatch = 9;
            const double row = 14;
            for (var i = 0; i < Runs.Count; i++)
            {
                var yy = y + i * row;
                dc.DrawRectangle(SolverBrush(i), null, new Rect(x, yy + 1, swatch, swatch));
                var name = Label(Runs[i].Name);
                var maxTextW = maxWidth - swatch - 4;
                if (name.Width > maxTextW) name.MaxTextWidth = Math.Max(10, maxTextW);
                dc.DrawText(name, new Point(x + swatch + 4, yy - 2));
            }
            return Runs.Count * row;
        }

        private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        private static Pen FrozenPen(Color c, double t) { var p = new Pen(Frozen(c), t); p.Freeze(); return p; }
    }
}
