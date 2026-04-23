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
    //
    // X-axis is log10 because solve times routinely span 4–5 decades (sub-ms
    // trivial boards vs. occasional multi-second full enumerations). On a
    // linear axis a single 100 s outlier compresses the useful 10–100 ms
    // range into one pixel; log space keeps every decade equally visible.
    internal sealed class TimeCdfChart : ChartBase
    {
        // Floor for log mapping — games that solve faster than this round up.
        // 0.1 ms is below the per-call stdio round-trip floor anyway, so the
        // visual loss is zero.
        private const double MinMs = 0.1;

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

            var rawMax = 0.0;
            for (var i = 0; i < Runs.Count; i++)
                for (var j = 0; j < Runs[i].Games.Count; j++)
                    if (Runs[i].Games[j].ElapsedMs > rawMax) rawMax = Runs[i].Games[j].ElapsedMs;
            if (rawMax <= 0) return;

            // Snap the axis to whole decades so ticks fall at round numbers.
            var logMin = Math.Log10(MinMs);
            var logMax = Math.Ceiling(Math.Log10(Math.Max(rawMax, MinMs * 10)));
            var logRange = logMax - logMin;
            if (logRange <= 0) return;

            double XForMs(double ms)
            {
                var clamped = Math.Max(MinMs, ms);
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

            // Vertical gridlines + labels at each whole decade (1 ms, 10 ms,
            // 100 ms, 1 s, 10 s, ...). Labels use compact units past 1000 ms.
            for (var d = (int)Math.Ceiling(logMin); d <= (int)logMax; d++)
            {
                var ms = Math.Pow(10, d);
                var x = XForMs(ms);
                dc.DrawLine(GridPen, new Point(x, padT), new Point(x, padT + plotH));
                var tick = Label(FormatMs(ms));
                dc.DrawText(tick, new Point(x - tick.Width / 2, origin.Y + 3));
            }

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
                        var x = XForMs(times[k]);
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

        // Compact axis tick labels. Sub-millisecond and sub-second go in ms;
        // seconds get their own unit so "100 s" reads cleaner than "100000 ms".
        private static string FormatMs(double ms)
        {
            if (ms < 1) return $"{ms:0.##} ms";
            if (ms < 1000) return $"{ms:0} ms";
            var s = ms / 1000.0;
            return s < 10 ? $"{s:0.#} s" : $"{s:0} s";
        }
    }
}
