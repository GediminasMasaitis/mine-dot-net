using System;
using System.Windows;
using System.Windows.Media;

namespace MineDotNet.GUI.Controls.Charts
{
    // One dot per solver at (avg solve time, win rate). Upper-left is
    // strictly better; any solver dominated (to the right AND below) by
    // another is Pareto-dominated and should be considered for removal.
    internal sealed class WinRateVsTimeScatter : ChartBase
    {
        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;

            var title = Label("Win rate vs avg time", 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));

            const double padL = 40, padR = 20, padT = 26, padB = 30;
            var plotW = w - padL - padR;
            var plotH = h - padT - padB;
            if (plotW <= 4 || plotH <= 4 || Runs.Count == 0) return;

            var maxMs = 0.0;
            for (var i = 0; i < Runs.Count; i++) if (Runs[i].AvgMs > maxMs) maxMs = Runs[i].AvgMs;
            if (maxMs <= 0) maxMs = 1;
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
                if (r.GamesPlayed == 0) continue;
                var x = padL + plotW * r.AvgMs / maxMs;
                var y = padT + plotH - plotH * r.WinRate;
                dc.DrawEllipse(SolverBrush(i), null, new Point(x, y), 6, 6);

                var text = Label(r.Name);
                // Flip label to the other side if we'd fall off the right edge.
                var lx = x + 10;
                if (lx + text.Width > padL + plotW) lx = x - 10 - text.Width;
                dc.DrawText(text, new Point(lx, y - text.Height / 2));
            }
        }
    }
}
