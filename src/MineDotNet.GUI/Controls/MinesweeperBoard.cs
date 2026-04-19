using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game.Models;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;

namespace MineDotNet.GUI.Controls
{
    internal sealed class MinesweeperBoard : FrameworkElement
    {
        private readonly ITileSource _tiles;
        private readonly IPaletteProvider _palette;

        private IReadOnlyMapBase<Cell> _map;
        private IList<Mask> _masks;
        private IDictionary<Coordinate, SolverResult> _results;

        private static readonly Brush EmptyCellBrush = Frozen(Color.FromRgb(28, 28, 32));
        // Three-point palette for probability overlays: bright green for
        // certain safes (0%), amber for uncertain middle probabilities,
        // bright red for certain mines (100%). Interpolated linearly in RGB
        // so a 20%-ish probability still reads as mostly-green-with-a-warning
        // and a 75% reads as mostly-red — quick visual triage at a glance.
        private static readonly Color ProbSafeColor = Color.FromRgb(110, 230, 130);
        private static readonly Color ProbMidColor = Color.FromRgb(235, 200, 90);
        private static readonly Color ProbMineColor = Color.FromRgb(235, 80, 80);
        private static readonly Typeface ProbabilityFace = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        private static Brush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public event EventHandler<BoardCellClickEventArgs> CellClick;

        public MinesweeperBoard(ITileSource tiles, IPaletteProvider palette)
        {
            _tiles = tiles;
            _palette = palette;
            Focusable = false;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            MinWidth = 200;
            MinHeight = 200;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            MouseUp += OnMouseUpHandler;
        }

        // FrameworkElement defaults DesiredSize to 0,0 — parents like ContentPresenter then
        // collapse us to nothing. Claim whatever space is offered so the board actually
        // stretches inside its Card host.
        protected override Size MeasureOverride(Size availableSize)
        {
            var w = double.IsInfinity(availableSize.Width) ? MinWidth : availableSize.Width;
            var h = double.IsInfinity(availableSize.Height) ? MinHeight : availableSize.Height;
            return new Size(w, h);
        }

        public void SetState(IReadOnlyMapBase<Cell> map, IList<Mask> masks, IDictionary<Coordinate, SolverResult> results)
        {
            _map = map;
            _masks = masks;
            _results = results;
            InvalidateVisual();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            // Fill full background so the dark surface shows through empty margins when the
            // map isn't a perfect multiple of available width/height.
            dc.DrawRectangle(EmptyCellBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (_map == null) return;

            var (cellSize, origin) = ComputeLayout();
            if (cellSize <= 0) return;

            var probFontSize = Math.Max(8.0, Math.Min(12.0, cellSize * 0.28));
            var probFace = ProbabilityFace;

            for (var x = 0; x < _map.Width; x++)
            {
                for (var y = 0; y < _map.Height; y++)
                {
                    var coord = new Coordinate(x, y);
                    var cell = _map[coord];
                    // X is row, Y is column — matches WinForms behaviour, so the board
                    // looks the same as before after the move to WPF.
                    var px = origin.X + y * cellSize;
                    var py = origin.Y + x * cellSize;
                    var rect = new Rect(px, py, cellSize, cellSize);
                    DrawCell(dc, cell, rect);
                    DrawMaskOverlays(dc, x, y, rect);
                    DrawProbability(dc, coord, rect, probFace, probFontSize);
                }
            }
        }

        private (double cellSize, Point origin) ComputeLayout()
        {
            var cellW = ActualWidth / _map.Height;
            var cellH = ActualHeight / _map.Width;
            var cell = Math.Floor(Math.Min(cellW, cellH));
            if (cell <= 0) return (0, new Point());
            var ox = (ActualWidth - cell * _map.Height) / 2.0;
            var oy = (ActualHeight - cell * _map.Width) / 2.0;
            return (cell, new Point(ox, oy));
        }

        private void DrawCell(DrawingContext dc, Cell cell, Rect rect)
        {
            dc.DrawRectangle(EmptyCellBrush, null, rect);

            ImageSource tile = null;
            if (cell.State == CellState.Empty)
            {
                _tiles.Hints.TryGetValue(cell.Hint, out tile);
            }
            else if (cell.Flag != CellFlag.None)
            {
                _tiles.Flags.TryGetValue(cell.Flag, out tile);
            }
            else if (cell.State == CellState.Filled && cell is GameCell gc && gc.HasMine)
            {
                tile = _tiles.UnrevealedMine;
            }
            else
            {
                _tiles.States.TryGetValue(cell.State, out tile);
            }

            if (tile != null)
            {
                dc.DrawImage(tile, rect);
            }
        }

        private void DrawMaskOverlays(DrawingContext dc, int x, int y, Rect rect)
        {
            if (_masks == null || _masks.Count == 0) return;
            var borderIncrement = (rect.Width / 2 - 5) / (_masks.Count + 1);
            var borderWidth = 0.0;
            for (var i = 0; i < _masks.Count; i++)
            {
                if (!_masks[i].Cells[x, y]) continue;
                var inner = new Rect(
                    rect.X + borderWidth + 1,
                    rect.Y + borderWidth + 1,
                    rect.Width - 2 * borderWidth - 1,
                    rect.Height - 2 * borderWidth - 1);
                dc.DrawRectangle(_palette.MaskOverlayBrushes[i], null, inner);
                borderWidth += borderIncrement;
            }
        }

        private static readonly Pen ProbabilityOutlinePen = FrozenPen(Color.FromRgb(18, 18, 22), 1.1);

        private void DrawProbability(DrawingContext dc, Coordinate coord, Rect rect, Typeface face, double fontSize)
        {
            if (_results == null) return;
            if (!_results.TryGetValue(coord, out var result)) return;
            var text = $"{result.Probability:##0.00%}";
            var brush = ProbabilityColorFor(result.Probability);
            var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                face, fontSize, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            // Paint a dark outline underneath via the geometry path, then the
            // filled text on top. Keeps the colour legible over any cell shade
            // (pale tile, red mine mask, green safe mask) without dimming the
            // fill itself — the outline does the contrast work.
            var origin = new Point(rect.X + 2, rect.Y + 2);
            var geom = ft.BuildGeometry(origin);
            dc.DrawGeometry(brush, ProbabilityOutlinePen, geom);
        }

        private static Pen FrozenPen(Color c, double thickness)
        {
            var pen = new Pen(new SolidColorBrush(c), thickness);
            pen.Brush.Freeze();
            pen.Freeze();
            return pen;
        }

        // Green-to-amber-to-red gradient keyed on mine probability. Two-stop
        // interpolation (safe→mid for p≤0.5, mid→mine for p>0.5) so the
        // middle reads cleanly as "uncertain" rather than a murky brown.
        private static Brush ProbabilityColorFor(double probability)
        {
            var p = Math.Min(1.0, Math.Max(0.0, probability));
            Color c = p <= 0.5
                ? Lerp(ProbSafeColor, ProbMidColor, p * 2.0)
                : Lerp(ProbMidColor, ProbMineColor, (p - 0.5) * 2.0);
            var brush = new SolidColorBrush(c);
            brush.Freeze();
            return brush;
        }

        private static Color Lerp(Color a, Color b, double t)
        {
            byte L(byte x, byte y) => (byte)(x + (y - x) * t);
            return Color.FromRgb(L(a.R, b.R), L(a.G, b.G), L(a.B, b.B));
        }

        private void OnMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (_map == null) return;
            var (cellSize, origin) = ComputeLayout();
            if (cellSize <= 0) return;
            var p = e.GetPosition(this);
            var col = (int)Math.Floor((p.X - origin.X) / cellSize);
            var row = (int)Math.Floor((p.Y - origin.Y) / cellSize);
            if (col < 0 || row < 0 || col >= _map.Height || row >= _map.Width) return;
            var coord = new Coordinate(row, col);
            CellClick?.Invoke(this, new BoardCellClickEventArgs(coord, e.ChangedButton));
        }
    }

    internal sealed class BoardCellClickEventArgs : EventArgs
    {
        public BoardCellClickEventArgs(Coordinate coord, MouseButton button)
        {
            Coordinate = coord;
            Button = button;
        }
        public Coordinate Coordinate { get; }
        public MouseButton Button { get; }
    }

}
