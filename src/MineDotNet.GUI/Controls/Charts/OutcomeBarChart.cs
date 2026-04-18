using System;
using System.Windows;
using System.Windows.Media;

namespace MineDotNet.GUI.Controls.Charts
{
    // Stacked bar per solver: Won (green, bottom), Stuck (yellow, middle),
    // Lost (red, top). Y axis is game count; bar total height = GamesPlayed.
    // Answers "which config is most reliable?" at a glance.
    internal sealed class OutcomeBarChart : ChartBase
    {
        private static readonly Brush WonBrush = Frozen(Color.FromRgb(110, 205, 130));
        private static readonly Brush StuckBrush = Frozen(Color.FromRgb(225, 205, 110));
        private static readonly Brush LostBrush = Frozen(Color.FromRgb(230, 100, 100));

        protected override void OnRender(DrawingContext dc)
        {
            var w = ActualWidth;
            var h = ActualHeight;

            var title = Label("Outcomes", 12, null, title: true);
            dc.DrawText(title, new Point(6, 4));

            // Inline legend below the title so segment colours are discoverable
            // without hovering. Three items small enough to fit on one line.
            DrawKey(dc, 6, 24);

            const double padL = 32, padR = 10, padT = 44, padB = 44;
            var plotW = w - padL - padR;
            var plotH = h - padT - padB;
            if (plotW <= 4 || plotH <= 4 || Runs.Count == 0) return;

            var maxN = 1;
            for (var i = 0; i < Runs.Count; i++) if (Runs[i].GamesPlayed > maxN) maxN = Runs[i].GamesPlayed;

            // Grid + y-axis ticks at 0, 50%, 100% of max.
            for (var i = 0; i <= 2; i++)
            {
                var v = (int)Math.Round(maxN * i / 2.0);
                var y = padT + plotH - plotH * i / 2.0;
                dc.DrawLine(GridPen, new Point(padL, y), new Point(padL + plotW, y));
                var tick = Label(v.ToString());
                dc.DrawText(tick, new Point(padL - tick.Width - 4, y - tick.Height / 2));
            }

            dc.DrawLine(AxisPen, new Point(padL, padT + plotH), new Point(padL + plotW, padT + plotH));
            dc.DrawLine(AxisPen, new Point(padL, padT), new Point(padL, padT + plotH));

            var slotW = plotW / Runs.Count;
            var barW = Math.Min(slotW * 0.65, 56);
            for (var i = 0; i < Runs.Count; i++)
            {
                var r = Runs[i];
                var centerX = padL + slotW * i + slotW / 2;
                var barX = centerX - barW / 2;
                var baseY = padT + plotH;

                var wonH = plotH * r.Won / (double)maxN;
                var stuckH = plotH * r.Stuck / (double)maxN;
                var lostH = plotH * r.Lost / (double)maxN;

                dc.DrawRectangle(WonBrush, null, new Rect(barX, baseY - wonH, barW, wonH));
                dc.DrawRectangle(StuckBrush, null, new Rect(barX, baseY - wonH - stuckH, barW, stuckH));
                dc.DrawRectangle(LostBrush, null, new Rect(barX, baseY - wonH - stuckH - lostH, barW, lostH));

                var name = Label(r.Name);
                name.MaxTextWidth = Math.Max(10, slotW - 4);
                name.Trimming = TextTrimming.CharacterEllipsis;
                dc.DrawText(name, new Point(centerX - Math.Min(name.Width, name.MaxTextWidth) / 2, baseY + 6));
            }
        }

        private void DrawKey(DrawingContext dc, double x, double y)
        {
            DrawKeyItem(dc, ref x, y, WonBrush, "won");
            x += 12;
            DrawKeyItem(dc, ref x, y, StuckBrush, "stuck");
            x += 12;
            DrawKeyItem(dc, ref x, y, LostBrush, "lost");
        }

        private void DrawKeyItem(DrawingContext dc, ref double x, double y, Brush brush, string label)
        {
            dc.DrawRectangle(brush, null, new Rect(x, y + 2, 10, 10));
            x += 14;
            var t = Label(label, 10, SubtleBrush);
            dc.DrawText(t, new Point(x, y - 1));
            x += t.Width;
        }

        private static Brush Frozen(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
    }
}
