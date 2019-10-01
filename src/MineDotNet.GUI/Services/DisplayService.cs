using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Tiles;

namespace MineDotNet.GUI.Services
{
    class DisplayService : IDisplayService
    {
        public event EventHandler<CellClickEventArgs> CellClick;
        public bool DrawCoordinates { get; set; }
        
        private readonly ITileProvider _tileProvider;
        private readonly IBrushProvider _brushProvider;

        private readonly Font _mainFont;
        private readonly Font _subFont;

        private PictureBox _target;
        private Size _currentCellSize;

        public DisplayService(ITileProvider tileProvider, IBrushProvider brushProvider)
        {
            _tileProvider = tileProvider;
            _brushProvider = brushProvider;

            _mainFont = new Font(FontFamily.GenericMonospace, 8, FontStyle.Bold);
            _subFont = new Font(FontFamily.GenericMonospace, 6, FontStyle.Bold);

            DrawCoordinates = true;
        }

        public void SetTarget(PictureBox target)
        {
            if (_target != null)
            {
                _target.MouseUp -= TargetOnClick;
            }

            _target = target;
            _target.MouseUp += TargetOnClick;
        }

        private void OnCellClick(CellClickEventArgs args)
        {
            CellClick?.Invoke(this, args);
        }

        private void TargetOnClick(object sender, MouseEventArgs eventArgs)
        {
            if (_currentCellSize.Width <= 0 || _currentCellSize.Height <= 0)
            {
                return;
            }
            if (!_target.Bounds.Contains(eventArgs.Location))
            {
                return;
            }

            var x = eventArgs.Location.Y/ _currentCellSize.Width;
            var y = eventArgs.Location.X/ _currentCellSize.Height;
            var coord = new Coordinate(x,y);
            var args = new CellClickEventArgs(coord, eventArgs.Button);
            OnCellClick(args);
        }

        private void DrawTile(Graphics graphics, Cell cell, Rectangle cellRectangle)
        {
            graphics.FillRectangle(_brushProvider.EmptyBrush, cellRectangle);
            var tiles = _tileProvider.GetTiles(cellRectangle.Size);
            Image tile;
            if (cell.State == CellState.Empty)
            {
                tile = tiles.Hints[cell.Hint];
            }
            else if (cell.Flag != CellFlag.None)
            {
                tile = tiles.Flags[cell.Flag];
            }
            else
            {
                tile = tiles.States[cell.State];
            }
            
            graphics.DrawImage(tile, cellRectangle);
        }

        private void DisplayCell(Graphics graphics, Cell cell, Size cellSize, Brush textBrush, IList<bool> masks, IDictionary<Coordinate, SolverResult> results)
        {
            var borderIncrement = (cellSize.Width / 2 - 5) / (masks.Count + 1);

            var cellX = cell.Y * cellSize.Width;
            var cellY = cell.X * cellSize.Height;
            var cellLocation = new Point(cellX, cellY);
            var cellRectangle = new Rectangle(cellLocation, cellSize);

            DrawTile(graphics, cell, cellRectangle);


            var borderWidth = 0;
            for(var i = 0; i < masks.Count; i++)
            {
                if(masks[i])
                {
                    graphics.FillRectangle(_brushProvider.Brushes[i], cellX + borderWidth + 1, cellY + borderWidth + 1, cellSize.Width - 2 * borderWidth - 1, cellSize.Height - 2 * borderWidth - 1);
                    borderWidth += borderIncrement;
                }
            }
            if(DrawCoordinates)
            {
                var posStr = $"[{cell.X};{cell.Y}]";
                graphics.DrawString(posStr, _mainFont, textBrush, cellX, cellY + cellSize.Height - 15);
            }

            if(results.TryGetValue(cell.Coordinate, out var result))
            {
                var probabilityStr = $"{result.Probability:##0.00%}";
                graphics.DrawString(probabilityStr, _mainFont, textBrush, cellX, cellY);
                if(result.HintProbabilities != null)
                {
                    var heightOffset = 2;
                    foreach(var resultHintProbability in result.HintProbabilities)
                    {
                        heightOffset += 8;
                        var hintProbabilityStr = $"{resultHintProbability.Key}:{resultHintProbability.Value:000.00%}";
                        graphics.DrawString(hintProbabilityStr, _subFont, textBrush, cellX, cellY + heightOffset);
                    }
                }
            }
        }

        private Size GetCellSize(IMap map)
        {
            var cellWidth = _target.Width / map.Height;
            var cellHeight = _target.Height / map.Width;
            if (cellHeight > cellWidth)
            {
                cellHeight = cellWidth;
            }
            if (cellWidth > cellHeight)
            {
                cellWidth = cellHeight;
            }

            return new Size(cellWidth, cellHeight);
        }

        public void DisplayMap(Map map, IList<MaskMap> masks, IDictionary<Coordinate, SolverResult> results = null)
        {
            if (results == null)
            {
                results = new Dictionary<Coordinate, SolverResult>();
            }
            var textColor = Color.DarkRed;
            var textBrush = new SolidBrush(textColor);
            var cellSize = GetCellSize(map);
            _currentCellSize = cellSize;

            var bmp = new Bitmap(_target.Width, _target.Height);
            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < map.Width; i++)
                {
                    for (var j = 0; j < map.Height; j++)
                    {
                        var cell = map.Cells[i, j];
                        var cellMasks = masks.Select(x => x.Cells[cell.X, cell.Y]).ToList();
                        DisplayCell(graphics, cell, cellSize, textBrush, cellMasks, results);
                    }
                }
            }
            _target.Image = bmp;
        }

        public void Dispose()
        {
            if (_target != null)
            {
                _target.MouseClick -= TargetOnClick;
            }
        }
    }
}