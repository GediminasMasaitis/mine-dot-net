using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MineDotNet.GUI.Controls.Charts
{
    // Cumulative distribution of per-game solve time, one step line per solver.
    // Reads as "fraction of games ≤ X ms." Way easier than overlaid histograms
    // when comparing 3+ configs — fast configs hug the left edge, slow tails
    // show as long rightward extensions.
    internal sealed class TimeCdfChart : ChartBase
    {
        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;

            var title = Label("Solve time CDF", 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));

            const double padL = 40, padR = 110, padT = 26, padB = 30;
            var plotW = w - padL - padR;
            var plotH = h - padT - padB;
            if (plotW <= 4 || plotH <= 4 || Runs.Count == 0) return;

            var maxMs = 0.0;
            for (var i = 0; i < Runs.Count; i++)
                for (var j = 0; j < Runs[i].Games.Count; j++)
                    if (Runs[i].Games[j].ElapsedMs > maxMs) maxMs = Runs[i].Games[j].ElapsedMs;
            if (maxMs <= 0) return;
            maxMs = NiceCeiling(maxMs);

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

            var t0 = Label("0");
            dc.DrawText(t0, new Point(padL - t0.Width / 2, origin.Y + 3));
            var tMax = Label($"{maxMs:F0} ms");
            dc.DrawText(tMax, new Point(padL + plotW - tMax.Width, origin.Y + 3));

            for (var i = 0; i < Runs.Count; i++)
            {
                var r = Runs[i];
                if (r.Games.Count == 0) continue;

                var times = r.Games.Select(g => g.ElapsedMs).OrderBy(x => x).ToArray();
                var n = times.Length;
                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new Point(padL, padT + plotH), false, false);
                    for (var k = 0; k < n; k++)
                    {
                        var x = padL + plotW * times[k] / maxMs;
                        // Step up at each sample: horizontal to new x at old percentile,
                        // then vertical to new percentile.
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
