using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MineDotNet.GUI.Models;

namespace MineDotNet.GUI.Controls.Charts
{
    // Iteration-count CDF split by outcome — two curves aggregated across
    // every game in every run: won (green) and lost (red). Answers "does
    // this config die early or die late?" Early losses (mass at low iters)
    // mean the solver's guesses are hitting mines in the first few moves.
    // Late losses (mass at high iters) mean it grinds through deductions
    // then runs out of verdicts and loses on the end-game guess.
    //
    // Not using ValueCdfChart as the base because that draws one curve per
    // run; here we want exactly two curves regardless of run count, grouped
    // by outcome across the whole dataset.
    internal sealed class OutcomeIterationsCdfChart : ChartBase
    {
        private static readonly Brush WonBrush = Frozen(Color.FromRgb(110, 205, 130));
        private static readonly Brush LostBrush = Frozen(Color.FromRgb(230, 100, 100));
        private static readonly Pen WonPen = FrozenPen(Color.FromRgb(110, 205, 130), 1.8);
        private static readonly Pen LostPen = FrozenPen(Color.FromRgb(230, 100, 100), 1.8);

        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;

            var title = Label("Iterations by outcome", 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));

            const double padL = 40, padR = 90, padT = 26, padB = 30;
            var plotW = w - padL - padR;
            var plotH = h - padT - padB;
            if (plotW <= 4 || plotH <= 4 || Runs.Count == 0) return;

            var won = new List<int>();
            var lost = new List<int>();
            var rawMax = 1;
            for (var i = 0; i < Runs.Count; i++)
            {
                for (var j = 0; j < Runs[i].Games.Count; j++)
                {
                    var g = Runs[i].Games[j];
                    if (g.Iterations > rawMax) rawMax = g.Iterations;
                    switch (g.Outcome)
                    {
                        case BenchmarkOutcome.Won: won.Add(g.Iterations); break;
                        case BenchmarkOutcome.Lost: lost.Add(g.Iterations); break;
                        // Stuck is so rare it'd be noise as its own line; drop it.
                    }
                }
            }
            if (won.Count == 0 && lost.Count == 0) return;

            const double MinValue = 1;
            var logMin = Math.Log10(MinValue);
            var logMax = Math.Ceiling(Math.Log10(Math.Max(rawMax, MinValue * 10)));
            var logRange = logMax - logMin;
            if (logRange <= 0) return;

            double X(double v)
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
                var x = X(value);
                dc.DrawLine(GridPen, new Point(x, padT), new Point(x, padT + plotH));
                var tick = Label($"{value:0}");
                dc.DrawText(tick, new Point(x - tick.Width / 2, origin.Y + 3));
            }

            DrawCurve(dc, won, WonPen, X, padL, padT, plotW, plotH);
            DrawCurve(dc, lost, LostPen, X, padL, padT, plotW, plotH);

            // Legend: just two rows, Won and Lost. Not using DrawLegend
            // because that reads from Runs — we're colouring by outcome, not
            // by solver.
            var legendX = padL + plotW + 10;
            var legendY = padT + 2;
            const double swatch = 9, row = 14;
            dc.DrawRectangle(WonBrush, null, new Rect(legendX, legendY + 1, swatch, swatch));
            dc.DrawText(Label($"Won ({won.Count})"), new Point(legendX + swatch + 4, legendY - 2));
            dc.DrawRectangle(LostBrush, null, new Rect(legendX, legendY + row + 1, swatch, swatch));
            dc.DrawText(Label($"Lost ({lost.Count})"), new Point(legendX + swatch + 4, legendY + row - 2));
        }

        private static void DrawCurve(DrawingContext dc, List<int> values, Pen pen,
            Func<double, double> xMap, double padL, double padT, double plotW, double plotH)
        {
            if (values.Count == 0) return;
            values.Sort();
            var n = values.Count;
            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(new Point(padL, padT + plotH), false, false);
                for (var k = 0; k < n; k++)
                {
                    var x = xMap(values[k]);
                    var yPrev = padT + plotH - plotH * k / (double)n;
                    var yNext = padT + plotH - plotH * (k + 1) / (double)n;
                    ctx.LineTo(new Point(x, yPrev), true, false);
                    ctx.LineTo(new Point(x, yNext), true, false);
                }
                ctx.LineTo(new Point(padL + plotW, padT), true, false);
            }
            geom.Freeze();
            dc.DrawGeometry(null, pen, geom);
        }

        private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        private static Pen FrozenPen(Color c, double t) { var p = new Pen(Frozen(c), t); p.Freeze(); return p; }
    }
}
