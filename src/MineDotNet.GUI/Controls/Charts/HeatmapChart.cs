using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // 2D colour grid for a two-axis sweep. Each cell is one (axisA, axisB)
    // combination; the cell colour encodes a per-run metric (win rate / avg
    // time / avg iterations). Reads way faster than a rotatable 3D surface
    // for spotting ridges, valleys, and "this parameter only matters at
    // high density" interaction patterns.
    //
    // Subclasses pick the metric extractor, the formatter for tick labels,
    // and optional fixed min/max (e.g. 0–100% for win rate). Uses the first
    // solver's runs only — with multiple solvers, pick which to display
    // via a per-solver picker on a future iteration.
    internal abstract class HeatmapChart : ChartBase
    {
        protected abstract string ChartTitle { get; }
        protected abstract double ExtractValue(BenchmarkSolverRun run);
        protected abstract string FormatLegendValue(double value);
        protected abstract double? FixedMin { get; }
        protected abstract double? FixedMax { get; }

        public string AxisNameA { get; set; } = "Axis A";
        public string AxisNameB { get; set; } = "Axis B";

        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;

            var title = Label(ChartTitle, 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));

            const double padL = 50, padR = 90, padT = 26, padB = 40;
            var plotW = w - padL - padR;
            var plotH = h - padT - padB;
            if (plotW <= 4 || plotH <= 4 || Runs.Count == 0) return;

            // Only use the first solver's runs — avoids mixing solvers into
            // one cell. Heatmap with 2 solvers ⇒ user picks the one they
            // care about or adds a second heatmap later.
            var byAxis = Runs
                .Where(r => r.SolverIndex == 0 && r.AxisValue.HasValue && r.AxisValueB.HasValue)
                .GroupBy(r => (a: r.AxisValue.Value, b: r.AxisValueB.Value))
                .ToDictionary(g => g.Key, g => g.First());
            if (byAxis.Count == 0)
            {
                var msg = Label("2D sweep required", 12, SubtleBrush);
                dc.DrawText(msg, new Point(padL + plotW / 2 - msg.Width / 2, padT + plotH / 2 - msg.Height / 2));
                return;
            }

            var axisA = byAxis.Keys.Select(k => k.a).Distinct().OrderBy(x => x).ToArray();
            var axisB = byAxis.Keys.Select(k => k.b).Distinct().OrderBy(x => x).ToArray();
            if (axisA.Length == 0 || axisB.Length == 0) return;

            // Value range: honour subclass's fixed bounds if set, else use
            // actual data. Falling back to data makes heatmaps auto-scale
            // for unbounded metrics like avg time.
            var dataValues = byAxis.Values
                .Select(r => r.GamesPlayed > 0 ? ExtractValue(r) : double.NaN)
                .Where(v => !double.IsNaN(v))
                .ToArray();
            if (dataValues.Length == 0) return;
            var vMin = FixedMin ?? dataValues.Min();
            var vMax = FixedMax ?? dataValues.Max();
            if (Math.Abs(vMax - vMin) < 1e-9) vMax = vMin + 1e-9;

            var cellW = plotW / axisA.Length;
            var cellH = plotH / axisB.Length;

            // Cells first — base layer.
            for (var xi = 0; xi < axisA.Length; xi++)
            {
                for (var yi = 0; yi < axisB.Length; yi++)
                {
                    if (!byAxis.TryGetValue((axisA[xi], axisB[yi]), out var run)) continue;
                    if (run.GamesPlayed == 0) continue;
                    var v = ExtractValue(run);
                    var t = (v - vMin) / (vMax - vMin);
                    t = Math.Max(0, Math.Min(1, t));
                    var brush = HeatColour(t);
                    var x = padL + xi * cellW;
                    var y = padT + plotH - (yi + 1) * cellH;
                    dc.DrawRectangle(brush, null, new Rect(x, y, cellW + 0.5, cellH + 0.5));
                }
            }

            // Axis ticks — cap at ~6 labels per axis so dense sweeps don't
            // pile text on top of itself.
            var origin = new Point(padL, padT + plotH);
            dc.DrawLine(AxisPen, origin, new Point(padL + plotW, origin.Y));
            dc.DrawLine(AxisPen, origin, new Point(origin.X, padT));

            var xStride = Math.Max(1, (int)Math.Ceiling(axisA.Length / 6.0));
            for (var xi = 0; xi < axisA.Length; xi += xStride)
            {
                var cx = padL + (xi + 0.5) * cellW;
                var tick = Label(FormatAxisValue(axisA[xi]));
                dc.DrawText(tick, new Point(cx - tick.Width / 2, origin.Y + 3));
            }
            var yStride = Math.Max(1, (int)Math.Ceiling(axisB.Length / 6.0));
            for (var yi = 0; yi < axisB.Length; yi += yStride)
            {
                var cy = padT + plotH - (yi + 0.5) * cellH;
                var tick = Label(FormatAxisValue(axisB[yi]));
                dc.DrawText(tick, new Point(padL - tick.Width - 4, cy - tick.Height / 2));
            }

            // Axis titles at the midpoints.
            var xTitle = Label(AxisNameA);
            dc.DrawText(xTitle, new Point(padL + plotW / 2 - xTitle.Width / 2, origin.Y + 18));
            var yTitle = Label(AxisNameB);
            dc.PushTransform(new RotateTransform(-90, 12, padT + plotH / 2));
            dc.DrawText(yTitle, new Point(12 - yTitle.Width / 2, padT + plotH / 2 - yTitle.Height / 2));
            dc.Pop();

            // Legend: vertical colour ramp on the right, with min/max labels.
            var legendX = padL + plotW + 12;
            var legendW = 14.0;
            var legendSteps = 40;
            for (var i = 0; i < legendSteps; i++)
            {
                var t = 1 - (i / (double)(legendSteps - 1));
                var y = padT + plotH * i / legendSteps;
                var stepH = plotH / legendSteps + 0.5;
                dc.DrawRectangle(HeatColour(t), null, new Rect(legendX, y, legendW, stepH));
            }
            var maxLabel = Label(FormatLegendValue(vMax));
            dc.DrawText(maxLabel, new Point(legendX + legendW + 4, padT - maxLabel.Height / 2 + 2));
            var minLabel = Label(FormatLegendValue(vMin));
            dc.DrawText(minLabel, new Point(legendX + legendW + 4, padT + plotH - minLabel.Height / 2 - 2));
        }

        // 0 → low (deep blue), 1 → high (bright red). Same gradient as the
        // 3D surface's height colouring so the two charts read with the
        // same mental map.
        private static Brush HeatColour(double t)
        {
            t = Math.Max(0, Math.Min(1, t));
            Color lerp(Color a, Color b, double s) => Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * s),
                (byte)(a.G + (b.G - a.G) * s),
                (byte)(a.B + (b.B - a.B) * s));
            Color c;
            if (t < 0.35) c = lerp(Color.FromRgb(40, 80, 150), Color.FromRgb(50, 160, 200), t / 0.35);
            else if (t < 0.7) c = lerp(Color.FromRgb(50, 160, 200), Color.FromRgb(220, 190, 90), (t - 0.35) / 0.35);
            else c = lerp(Color.FromRgb(220, 190, 90), Color.FromRgb(200, 60, 60), (t - 0.7) / 0.3);
            var b = new SolidColorBrush(c); b.Freeze();
            return b;
        }

        // Shared with the 3D chart — density fractions shown as %, otherwise
        // integer or up to two decimals.
        private static string FormatAxisValue(double v)
        {
            if (v > 0 && v < 1) return $"{v * 100:0.#}%";
            return Math.Abs(v - Math.Round(v)) < 1e-6 ? $"{v:0}" : $"{v:0.##}";
        }
    }

    internal sealed class WinRateHeatmap : HeatmapChart
    {
        protected override string ChartTitle => "Win rate heatmap";
        protected override double ExtractValue(BenchmarkSolverRun run) => run.WinRate * 100;
        protected override string FormatLegendValue(double v) => $"{v:0}%";
        protected override double? FixedMin => 0;
        protected override double? FixedMax => 100;
    }

    internal sealed class AvgTimeHeatmap : HeatmapChart
    {
        protected override string ChartTitle => "Avg time heatmap";
        protected override double ExtractValue(BenchmarkSolverRun run) => run.AvgMs;
        protected override string FormatLegendValue(double v) => $"{v:0} ms";
        protected override double? FixedMin => null;
        protected override double? FixedMax => null;
    }

    internal sealed class AvgIterationsHeatmap : HeatmapChart
    {
        protected override string ChartTitle => "Avg iterations heatmap";
        protected override double ExtractValue(BenchmarkSolverRun run) => run.AvgIterations;
        protected override string FormatLegendValue(double v) => $"{v:0.#}";
        protected override double? FixedMin => null;
        protected override double? FixedMax => null;
    }
}
