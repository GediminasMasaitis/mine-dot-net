using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Tiles;

namespace MineDotNet.GUI.Services
{
    class DisplayService : IDisposable
    {
        public bool DrawCoordinates { get; set; }
        public PictureBox Target { get; }
        public TextMapVisualizer Visualizer { get; set; }

        public IList<SolidBrush> Brushes { get; }
        private SolidBrush EmptyBrush { get; }


        private int CurrentCellWidth { get; set; }
        private int CurrentCellHeight { get; set; }

        public event EventHandler<CellClickEventArgs> CellClick;

        private readonly ITileProvider _tileProvider;

        public DisplayService(PictureBox target, int colorCount, TextMapVisualizer visualizer)
        {
            var tileLoader = new TileLoader();
            var tileResizer = new TileResizer();
            var tileGenerator = new TileGenerator();
            _tileProvider = new TileProvider(tileLoader, tileResizer, tileGenerator);

            DrawCoordinates = true;
            Target = target;
            Visualizer = visualizer;

            var colors = new List<Color>
            {
                //Color.FromArgb(0, 0, 0),
                Color.FromArgb(100, 0, 150, 0),
                Color.FromArgb(100, 150, 0, 0),
                Color.FromArgb(100, 40, 70, 220),
                Color.FromArgb(100,100, 100, 0),
                Color.FromArgb(100,100, 0, 100),
                Color.FromArgb(100,0, 100, 100),
                Color.FromArgb(100,170, 70, 0),
                Color.FromArgb(100,0, 170, 100),
                Color.FromArgb(100,70, 30, 0),
                Color.FromArgb(100,180, 0, 100),
                Color.FromArgb(100,180, 150, 50),
                Color.FromArgb(100,120, 120, 120),
                Color.FromArgb(100,170, 170, 170),
            };

            var rng = new Random(0);
            while (colors.Count < colorCount)
            {
                var red = rng.Next(0, 256);
                var green = rng.Next(0, 256);
                var blue = rng.Next(0, 256);
                var color = Color.FromArgb(red, green, blue);
                colors.Add(color);
            }

            Brushes = colors.Select(x => new SolidBrush(x)).ToList();

            EmptyBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            Target.MouseUp += TargetOnClick;
        }

        protected void OnCellClick(CellClickEventArgs args)
        {
            CellClick?.Invoke(this, args);
        }

        private void TargetOnClick(object sender, MouseEventArgs eventArgs)
        {
            if (CurrentCellWidth <= 0 || CurrentCellHeight <= 0)
            {
                return;
            }
            if (!Target.Bounds.Contains(eventArgs.Location))
            {
                return;
            }

            var x = eventArgs.Location.Y/CurrentCellWidth;
            var y = eventArgs.Location.X/CurrentCellHeight;
            var coord = new Coordinate(x,y);
            var args = new CellClickEventArgs(coord, eventArgs.Button);
            OnCellClick(args);
        }

        private void DrawTile(Graphics graphics, Cell cell, int cellX, int cellY, int width, int height)
        {
            graphics.FillRectangle(EmptyBrush, cellX, cellY, width, height);
            var tiles = _tileProvider.GetTiles(width, height);
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

            graphics.DrawImage(tile, cellX, cellY, width, height);
        }

        public void DisplayCell(Graphics graphics, Cell cell, int cellWidth, int cellHeight, Brush textBrush, IList<bool> masks, IDictionary<Coordinate, SolverResult> results, Font mainFont, Font subFont)
        {
            var borderIncrement = (cellWidth / 2 - 5) / (masks.Count + 1);

            var cellX = cell.Y * cellWidth;
            var cellY = cell.X * cellHeight;
            DrawTile(graphics, cell, cellX, cellY, cellWidth, cellHeight);


            var borderWidth = 0;
            for(var i = 0; i < masks.Count; i++)
            {
                if(masks[i])
                {
                    graphics.FillRectangle(Brushes[i], cellX + borderWidth + 1, cellY + borderWidth + 1, cellWidth - 2 * borderWidth - 1, cellHeight - 2 * borderWidth - 1);
                    borderWidth += borderIncrement;
                }
            }
            if(DrawCoordinates)
            {
                var posStr = $"[{cell.X};{cell.Y}]";
                graphics.DrawString(posStr, mainFont, textBrush, cellX, cellY + cellHeight - 15);
            }

            if(results.TryGetValue(cell.Coordinate, out var result))
            {
                var probabilityStr = $"{result.Probability:##0.00%}";
                graphics.DrawString(probabilityStr, mainFont, textBrush, cellX, cellY);
                if(result.HintProbabilities != null)
                {
                    var heightOffset = 2;
                    foreach(var resultHintProbability in result.HintProbabilities)
                    {
                        heightOffset += 8;
                        var hintProbabilityStr = $"{resultHintProbability.Key}:{resultHintProbability.Value:000.00%}";
                        graphics.DrawString(hintProbabilityStr, subFont, textBrush, cellX, cellY + heightOffset);
                    }
                }
            }
        }

        public void DisplayMap(Map map, IList<MaskMap> masks, IDictionary<Coordinate, SolverResult> results = null)
        {
            if (results == null)
            {
                results = new Dictionary<Coordinate, SolverResult>();
            }
            var textColor = Color.DarkRed;
            var textBrush = new SolidBrush(textColor);
            var cellWidth = Target.Width/map.Height;
            var cellHeight = Target.Height/map.Width;
            if (cellHeight > cellWidth)
            {
                cellHeight = cellWidth;
            }
            if (cellWidth > cellHeight)
            {
                cellWidth = cellHeight;
            }

            var mainFont = new Font(FontFamily.GenericMonospace, 8, FontStyle.Bold);
            var subFont = new Font(FontFamily.GenericMonospace, 6, FontStyle.Bold);

            var bmp = new Bitmap(Target.Width, Target.Height);
            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < map.Width; i++)
                {
                    for (var j = 0; j < map.Height; j++)
                    {
                        var cell = map.Cells[i, j];
                        var cellMasks = masks.Select(x => x.Cells[cell.X, cell.Y]).ToList();
                        DisplayCell(graphics, cell, cellWidth, cellHeight, textBrush, cellMasks, results, mainFont, subFont);
                    }
                }
            }
            Target.Image = bmp;
        }

        public void Dispose()
        {
            Target.MouseClick -= TargetOnClick;
        }
    }
}