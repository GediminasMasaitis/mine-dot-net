using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using MineDotNet.AI;
using MineDotNet.Common;

namespace MineDotNet.GUI
{
    class DisplayService
    {
        public PictureBox Target { get; set; }
        public TextMapVisualizer Visualizer { get; set; }

        public IList<SolidBrush> Brushes { get; }
        private SolidBrush EmptyBrush { get; }

        private IDictionary<int, Image> HintTextures { get; set; }
        private IDictionary<CellState, Image> StateTextures { get; set; }
        private IDictionary<CellFlag, Image> FlagTextures { get; set; }
        private IDictionary<int, Image> ResizedHintTextures { get; set; }
        private IDictionary<CellState, Image> ResizedStateTextures { get; set; }
        private IDictionary<CellFlag, Image> ResizedFlagTextures { get; set; }
        private int CurrentTextureWidth { get; set; }
        private int CurrentTextureHeight { get; set; }

        public DisplayService(PictureBox target, int colorCount, TextMapVisualizer visualizer)
        {
            Target = target;
            Visualizer = visualizer;

            var colors = new List<Color>
            {
                Color.FromArgb(0, 0, 0),
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
        }

        public void TryLoadAssets()
        {
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var path = $@"{currentPath}\assets";
            HintTextures = new Dictionary<int, Image>();
            FlagTextures = new Dictionary<CellFlag, Image>();
            StateTextures = new Dictionary<CellState, Image>();
            if (!Directory.Exists(path))
            {
                return;
            }
            for (var i = 1; i <= 8; i++)
            {
                var hintPath = $@"{path}\{i}.png";
                if (File.Exists(hintPath))
                {
                    HintTextures[i] = Image.FromFile(hintPath);
                }
            }

            var flagPath = $@"{path}\flag.png";
            if (File.Exists(flagPath))
            {
                FlagTextures[CellFlag.HasMine] = Image.FromFile(flagPath);
            }

            var unknownPath = $@"{path}\unknown.png";
            if (File.Exists(unknownPath))
            {
                FlagTextures[CellFlag.NotSure] = Image.FromFile(unknownPath);
            }

            var filledPath = $@"{path}\filled.png";
            if (File.Exists(filledPath))
            {
                StateTextures[CellState.Filled] = Image.FromFile(filledPath);
            }

            var emptyPath = $@"{path}\empty.png";
            if (File.Exists(emptyPath))
            {
                StateTextures[CellState.Empty] = Image.FromFile(emptyPath);
            }
        }

        private Image ResizeImage(Image image, int width, int height)
        {
            //return new Bitmap(image, width, height);
            var rect = new Rectangle(0, 0, width, height);
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = InterpolationMode.High;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var attr = new ImageAttributes())
                {
                    attr.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attr);
                }
            }
            return bitmap;
        }

        private void RescaleTiles(int width, int height)
        {
            if (CurrentTextureWidth == width && CurrentTextureHeight == height)
            {
                return;
            }
            ResizedFlagTextures = new Dictionary<CellFlag, Image>();
            ResizedHintTextures = new Dictionary<int, Image>();
            ResizedStateTextures = new Dictionary<CellState, Image>();

            foreach (var texture in StateTextures)
            {
                ResizedStateTextures[texture.Key] = ResizeImage(texture.Value, width, height);
            }
            foreach (var texture in HintTextures)
            {
                ResizedHintTextures[texture.Key] = ResizeImage(texture.Value, width, height);
            }
            foreach (var texture in FlagTextures)
            {
                ResizedFlagTextures[texture.Key] = ResizeImage(texture.Value, width, height);
            }
            CurrentTextureWidth = width;
            CurrentTextureHeight = height;
        }

        private void DisplayCell(Graphics graphics, Cell cell, int x, int y, int width, int height, Brush textBrush)
        {
            var tiles = new List<Image>();
            Image tempTile;
            var needStr = true;
            if (ResizedStateTextures.TryGetValue(cell.State, out tempTile))
            {
                tiles.Add(tempTile);
            }
            if (ResizedFlagTextures.TryGetValue(cell.Flag, out tempTile))
            {
                tiles.Add(tempTile);
                needStr = false;
            }
            if (ResizedHintTextures.TryGetValue(cell.Hint, out tempTile))
            {
                tiles.Add(tempTile);
                needStr = false;
            }
            needStr = needStr && cell.Hint != 0 && cell.Flag != CellFlag.None;
            foreach (var tile in tiles)
            {
                graphics.DrawImage(tile, x, y, width, height);
            }
            if (!needStr)
            {
                return;
            }
            var str = Visualizer.VisualizeCell(cell);
            var font = new Font(FontFamily.GenericMonospace, 12, FontStyle.Bold);
            if (str != null)
            {
                graphics.DrawString(str, font, textBrush, x + width/2 - 12, y + height/2 - 7);
            }
        }

        public void DisplayMaps(Map[] maps, IDictionary<Coordinate, SolverResult> results = null)
        {
            if (results == null)
            {
                results = new Dictionary<Coordinate, SolverResult>();
            }
            var textColor = Color.DarkRed;
            var textBrush = new SolidBrush(textColor);
            var cellWidth = Target.Width/maps[0].Height;
            var cellHeight = Target.Height/maps[0].Width;
            if (cellHeight > cellWidth)
            {
                cellHeight = cellWidth;
            }
            if (cellWidth > cellHeight)
            {
                cellWidth = cellHeight;
            }
            RescaleTiles(cellHeight, cellWidth);

            var debugTextFont = new Font(FontFamily.GenericMonospace, 8, FontStyle.Bold);

            var borderIncrement = (cellWidth/2 - 5)/maps.Length;

            var bmp = new Bitmap(Target.Width, Target.Height);
            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < maps[0].Width; i++)
                {
                    for (var j = 0; j < maps[0].Height; j++)
                    {

                        graphics.FillRectangle(EmptyBrush, j*cellWidth, i*cellHeight, cellWidth, cellHeight);
                        var cell = maps[0].Cells[i, j];
                        DisplayCell(graphics, cell, j*cellWidth, i*cellHeight, cellWidth, cellHeight, textBrush);

                        var borderWidth = 0;
                        for (var k = 1; k < maps.Length; k++)
                        {
                            if (maps[k]?.Cells[i, j].State == CellState.Filled)
                            {
                                graphics.FillRectangle(Brushes[k], j*cellWidth + borderWidth + 1, i*cellHeight + borderWidth + 1, cellWidth - 2*borderWidth - 1, cellHeight - 2*borderWidth - 1);
                                borderWidth += borderIncrement;
                            }
                        }
                        var posStr = $"[{i};{j}]";
                        graphics.DrawString(posStr, debugTextFont, textBrush, j*cellWidth, i*cellHeight);
                        SolverResult result;
                        if (results.TryGetValue(cell.Coordinate, out result))
                        {
                            var probabilityStr = result.Probability.ToString("##0.00%");
                            graphics.DrawString(probabilityStr, debugTextFont, textBrush, j*cellWidth, i*cellHeight + cellHeight - 15);
                        }
                    }

                    //for (var i = 0; i <= maps[0].Height; i++)
                    //{
                    //    graphics.DrawLine(Pens.Black, cellWidth*i, 0, cellWidth*i, cellHeight*maps[0].Width);
                    //}
                    //for (var i = 0; i <= maps[0].Width; i++)
                    //{
                    //    graphics.DrawLine(Pens.Black, 0, cellHeight * i, cellHeight * maps[0].Height, cellHeight * i);
                    //}
                }
            }
            Target.Image = bmp;
            //Target.Invalidate();
        }
    }
}