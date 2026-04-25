using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Shared base for "distribution of per-game X" charts. Subclasses pick
    // the value to extract (time, iterations, time-per-iteration, ...) and
    // the formatting for axis labels. Rendering — log10 x-axis, decade grid,
    // step lines per solver — lives here so every CDF we add looks the same.
    internal abstract class ValueCdfChart : ChartBase
    {
        protected abstract string ChartTitle { get; }
        protected abstract double ExtractValue(BenchmarkGameResult game);
        protected abstract string FormatTick(double value);
        // Hard floor for the log axis so log(0) never fires. Subclasses set
        // this to whatever's sensible for their unit (0.01 ms, 1 iteration).
        protected abstract double MinValue { get; }

        public override bool BenefitsFromSweepAggregation => true;

        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;

            var title = Label(ChartTitle, 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));

            const double padL = 40, padR = 110, padT = 26, padB = 30;
            var plotW = w - padL - padR;
            var plotH = h - padT - padB;
            if (plotW <= 4 || plotH <= 4 || Runs.Count == 0) return;

            var rawMax = 0.0;
            for (var i = 0; i < Runs.Count; i++)
                for (var j = 0; j < Runs[i].Games.Count; j++)
                {
                    var v = ExtractValue(Runs[i].Games[j]);
                    if (v > rawMax) rawMax = v;
                }
            if (rawMax <= 0) return;

            // Snap to whole decades so ticks fall at round numbers. Use the
            // subclass's MinValue as the axis floor — fixed, so the same
            // pixel position always means the same value across runs.
            var logMin = Math.Log10(MinValue);
            var logMax = Math.Ceiling(Math.Log10(Math.Max(rawMax, MinValue * 10)));
            var logRange = logMax - logMin;
            if (logRange <= 0) return;

            double XForValue(double v)
            {
                var clamped = Math.Max(MinValue, v);
                return padL + plotW * (Math.Log10(clamped) - logMin) / logRange;
            }

            for (var p = 0; p <= 100; p += 25)
            {
                var y = padT + plotH - plotH * p / 100.0;
                dc.DrawLine(GridPen, new Point(padL, y), new Point(padL + plotW, y));
                var tick = Label($"{p}%");
                dc.DrawText(tick, new Point(padL - tick.Width - 4, y - tick.Height / 2));
            }

            var origin = new Point(padL, padT + plotH);
            dc.DrawLine(AxisPen, origin, new Point(padL + plotW, origin.Y));
            dc.DrawLine(AxisPen, origin, new Point(origin.X, padT));

            for (var d = (int)Math.Ceiling(logMin); d <= (int)logMax; d++)
            {
                var value = Math.Pow(10, d);
                var x = XForValue(value);
                dc.DrawLine(GridPen, new Point(x, padT), new Point(x, padT + plotH));
                var tick = Label(FormatTick(value));
                dc.DrawText(tick, new Point(x - tick.Width / 2, origin.Y + 3));
            }

            for (var i = 0; i < Runs.Count; i++)
            {
                var r = Runs[i];
                if (r.Games.Count == 0) continue;

                var values = r.Games.Select(ExtractValue).OrderBy(x => x).ToArray();
                var n = values.Length;
                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new Point(padL, padT + plotH), false, false);
                    for (var k = 0; k < n; k++)
                    {
                        var x = XForValue(values[k]);
                        var yPrev = padT + plotH - plotH * k / (double)n;
                        var yNext = padT + plotH - plotH * (k + 1) / (double)n;
                        ctx.LineTo(new Point(x, yPrev), true, false);
                        ctx.LineTo(new Point(x, yNext), true, false);
                    }
                    ctx.LineTo(new Point(padL + plotW, padT), true, false);
                }
                geom.Freeze();
                dc.DrawGeometry(null, SolverPen(i, 1.8), geom);
            }

            DrawLegend(dc, padL + plotW + 10, padT + 2, padR - 10);
        }
    }
}
