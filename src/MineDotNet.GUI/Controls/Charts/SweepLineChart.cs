using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Line chart for sweep-mode benchmark results. One line per solver;
    // x-axis = sweep parameter value, y-axis = a per-run metric extracted by
    // the subclass. All three sweep charts share this shell so axes/legend
    // look consistent — only the title, y-label, and extractor differ.
    internal abstract class SweepLineChart : ChartBase
    {
        // Human-readable name of the swept axis (e.g. "Mine density",
        // "GiveUpFromSize"). Used in the title/x-label. Shared here so the
        // dialog can set it uniformly after swapping charts in/out dynamically.
        public string AxisName { get; set; } = "Parameter";

        protected abstract string ChartTitle { get; }
        protected abstract string XAxisLabel { get; }
        protected abstract string YAxisFormat { get; }       // "F0", "F1", etc.
        protected abstract string YAxisUnit { get; }          // "%", " ms", ""
        protected abstract double? FixedYMax { get; }          // 100 for %, null = auto
        protected abstract double ExtractY(BenchmarkSolverRun run);

        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;

            var title = Label(ChartTitle, 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));

            const double padL = 44, padR = 110, padT = 26, padB = 36;
            var plotW = w - padL - padR;
            var plotH = h - padT - padB;
            if (plotW <= 4 || plotH <= 4 || Runs.Count == 0) return;

            // X-axis range spans the FULL configured sweep (including axis values
            // not yet started) so the chart doesn't rescale every time a new
            // density finishes. Points fill in left-to-right as data arrives.
            var allAxisValues = Runs
                .Where(r => r.AxisValue.HasValue)
                .Select(r => r.AxisValue.Value)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
            if (allAxisValues.Length == 0) return;
            var xMin = allAxisValues.First();
            var xMax = allAxisValues.Last();
            if (xMax <= xMin) xMax = xMin + 1;

            // Only plot solvers that have at least one completed game — but keep
            // them per-solver grouped, one polyline per solver across whatever
            // axis values have data so far.
            var series = Runs
                .Where(r => r.AxisValue.HasValue && r.GamesPlayed > 0)
                .GroupBy(r => r.SolverIndex)
                .Select(g => new
                {
                    SolverIndex = g.Key,
                    Name = g.First().Name,
                    Points = g.OrderBy(r => r.AxisValue.Value)
                              .Select(r => (X: r.AxisValue.Value, Y: ExtractY(r)))
                              .ToArray()
                })
                .ToArray();

            // Guard yMax when we haven't got any data yet — first frame after
            // Run click renders the axes empty while games spin up.
            var maxY = series.SelectMany(s => s.Points).Select(p => p.Y).DefaultIfEmpty(0).Max();
            var yMax = FixedYMax ?? NiceCeiling(maxY);
            if (yMax <= 0) yMax = 1;

            for (var i = 0; i <= 4; i++)
            {
                var y = padT + plotH - plotH * i / 4.0;
                dc.DrawLine(GridPen, new Point(padL, y), new Point(padL + plotW, y));
                var val = yMax * i / 4.0;
                var tick = Label(val.ToString(YAxisFormat) + YAxisUnit);
                dc.DrawText(tick, new Point(padL - tick.Width - 4, y - tick.Height / 2));
            }

            var origin = new Point(padL, padT + plotH);
            dc.DrawLine(AxisPen, origin, new Point(padL + plotW, origin.Y));
            dc.DrawLine(AxisPen, origin, new Point(origin.X, padT));

            // X ticks at every configured sweep value (including those with no
            // data yet). Sweep is discrete, so ticks land on real points.
            foreach (var xv in allAxisValues)
            {
                var px = padL + plotW * (xv - xMin) / (xMax - xMin);
                dc.DrawLine(GridPen, new Point(px, padT + plotH), new Point(px, padT + plotH + 3));
                var tickLabel = XAxisLabel == "Mine density" ? $"{xv * 100:F0}%" : $"{xv:F0}";
                var tick = Label(tickLabel);
                // Skip tick labels that'd overlap their neighbour — keeps the
                // axis readable when the step count is high.
                dc.DrawText(tick, new Point(px - tick.Width / 2, padT + plotH + 4));
            }

            foreach (var s in series)
            {
                if (s.Points.Length == 0) continue;
                var pen = SolverPen(s.SolverIndex, 2);
                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    var first = true;
                    foreach (var pt in s.Points)
                    {
                        var px = padL + plotW * (pt.X - xMin) / (xMax - xMin);
                        var py = padT + plotH - plotH * pt.Y / yMax;
                        if (first) { ctx.BeginFigure(new Point(px, py), false, false); first = false; }
                        else ctx.LineTo(new Point(px, py), true, true);
                    }
                }
                geom.Freeze();
                dc.DrawGeometry(null, pen, geom);

                // Data point dots on top of the line.
                foreach (var pt in s.Points)
                {
                    var px = padL + plotW * (pt.X - xMin) / (xMax - xMin);
                    var py = padT + plotH - plotH * pt.Y / yMax;
                    dc.DrawEllipse(SolverBrush(s.SolverIndex), null, new Point(px, py), 3, 3);
                }
            }

            // Legend — one row per solver, in the right-hand margin.
            var solverNames = series.OrderBy(s => s.SolverIndex).ToArray();
            for (var i = 0; i < solverNames.Length; i++)
            {
                var y = padT + 2 + i * 14;
                dc.DrawRectangle(SolverBrush(solverNames[i].SolverIndex), null, new Rect(padL + plotW + 10, y + 1, 10, 10));
                var nameLabel = Label(solverNames[i].Name);
                nameLabel.MaxTextWidth = Math.Max(10, padR - 24);
                nameLabel.Trimming = TextTrimming.CharacterEllipsis;
                dc.DrawText(nameLabel, new Point(padL + plotW + 24, y - 2));
            }
        }
    }

    internal sealed class WinRateSweepChart : SweepLineChart
    {
        protected override string ChartTitle => $"Win rate vs {AxisName.ToLowerInvariant()}";
        protected override string XAxisLabel => AxisName;
        protected override string YAxisFormat => "F0";
        protected override string YAxisUnit => "%";
        protected override double? FixedYMax => 100;
        protected override double ExtractY(BenchmarkSolverRun run) => run.WinRate * 100;
    }

    internal sealed class AvgTimeSweepChart : SweepLineChart
    {
        protected override string ChartTitle => $"Avg time vs {AxisName.ToLowerInvariant()}";
        protected override string XAxisLabel => AxisName;
        protected override string YAxisFormat => "F0";
        protected override string YAxisUnit => " ms";
        protected override double? FixedYMax => null;
        protected override double ExtractY(BenchmarkSolverRun run) => run.AvgMs;
    }

    internal sealed class AvgIterationsSweepChart : SweepLineChart
    {
        protected override string ChartTitle => $"Avg iterations vs {AxisName.ToLowerInvariant()}";
        protected override string XAxisLabel => AxisName;
        protected override string YAxisFormat => "F1";
        protected override string YAxisUnit => "";
        protected override double? FixedYMax => null;
        protected override double ExtractY(BenchmarkSolverRun run) => run.AvgIterations;
    }
}
