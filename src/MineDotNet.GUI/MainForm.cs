using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using MineDotNet.AI;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;

namespace MineDotNet.GUI
{
    public partial class MainForm : Form
    {
        private IList<TextBox> MapTextBoxes { get; }
        private IList<SolidBrush> Brushes { get;}
        private SolidBrush EmptyBrush { get; }
        private TextMapParser Parser { get; }
        private TextMapVisualizer Visualizer { get; }
        private int MapCount { get; }

        private IDictionary<int, Image> HintTextures { get; set; }
        private IDictionary<CellState, Image> StateTextures { get; set; }
        private IDictionary<CellFlag, Image> FlagTextures { get; set; }
        private IDictionary<int, Image> ResizedHintTextures { get; set; }
        private IDictionary<CellState, Image> ResizedStateTextures { get; set; }
        private IDictionary<CellFlag, Image> ResizedFlagTextures { get; set; }
        private int CurrentTextureWidth { get; set; }
        private int CurrentTextureHeight { get; set; }

        public MainForm() : this(new Map[0])
        {
        }

        public MainForm(IList<Map> maps)
        {
            InitializeComponent();
            Parser = new TextMapParser();
            Visualizer = new TextMapVisualizer();
            var allMaps = maps.ToList();
            MapCount = 3;
            MapTextBoxes = new TextBox[MapCount];
            MapTextBoxes[0] = Map0TextBox;

            var colors = new List<Color>
            {
                Color.FromArgb(0, 0, 0),
                Color.FromArgb(100, 0, 150, 0),
                Color.FromArgb(100, 150, 0, 0),
                Color.FromArgb(100, 40, 70, 220),
                //Color.FromArgb(100, 100, 0),
                //Color.FromArgb(100, 0, 100),
                //Color.FromArgb(0, 100, 100),
                //Color.FromArgb(170, 70, 0),
                //Color.FromArgb(0, 170, 100),
                //Color.FromArgb(70, 30, 0),
                //Color.FromArgb(180, 0, 100),
                //Color.FromArgb(180, 150, 50),
                //Color.FromArgb(120, 120, 120),
                //Color.FromArgb(170, 170, 170),
            };

            var rng = new Random(0);
            while (colors.Count < MapCount)
            {
                var red = rng.Next(0, 256);
                var green = rng.Next(0, 256);
                var blue = rng.Next(0, 256);
                var color = Color.FromArgb(red, green, blue);
                colors.Add(color);
            }

            Brushes = colors.Select(x => new SolidBrush(x)).ToList();
            EmptyBrush = new SolidBrush(Color.FromArgb(100, 100, 100));

            var offsetX = 170;
            var offsetY = 0;

            for (var i = 1; i < MapCount; i++)
            {
                var newTextBox = new TextBox();
                newTextBox.Parent = this;
                newTextBox.Location = new Point(Map0TextBox.Location.X + offsetX*i, Map0TextBox.Location.Y + offsetY*i);
                newTextBox.Multiline = Map0TextBox.Multiline;
                newTextBox.Size = Map0TextBox.Size;
                newTextBox.Anchor = Map0TextBox.Anchor;
                newTextBox.AcceptsReturn = Map0TextBox.AcceptsReturn;

                var newLabel = new Label();
                newLabel.Parent = this;
                newLabel.Location = new Point(Map0Label.Location.X + offsetX*i, Map0Label.Location.Y + offsetY*i);
                newLabel.Text = $"Mask {i}:";
                newLabel.ForeColor = Brushes[i].Color;
                newLabel.Anchor = Map0Label.Anchor;

                MapTextBoxes[i] = newTextBox;
            }

            if (allMaps.Count == 0)
            {
                allMaps.Add(new Map(8,8,true));
            }


            for (var i = 0; i < MapTextBoxes.Count; i++)
            {
                MapTextBoxes[i].ForeColor = Brushes[i].Color;
                MapTextBoxes[i].Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Bold);
            }

            for (var i = 0; i < allMaps.Count; i++)
            {
                if (i >= MapTextBoxes.Count)
                {
                    break;
                }
                var mapStr = Visualizer.VisualizeToString(allMaps[i]);
                MapTextBoxes[i].Text = mapStr;
            }
            TryLoadAssets();
            DisplayMaps(allMaps.ToArray());
        }

        private void TryLoadAssets()
        {
            var path = @".\assets";
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

        private void ShowMapsButton_Click(object sender, EventArgs e)
        {
            DisplayMaps();
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
            foreach(var tile in tiles)
            {
                graphics.DrawImage(tile, x, y, width, height);
            }
            if (!needStr)
            {
                return;
            }
            var str = Visualizer.VisualizeCell(cell);
            var font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            if (str != null)
            {
                graphics.DrawString(str, font, textBrush, x + width/2 - 12, y + height/2 - 7);
            }
        }

        private void DisplayMaps(Map[] maps = null, IDictionary<Coordinate, decimal> probabilities = null)
        {
            if (maps == null)
            {
                maps = new Map[MapTextBoxes.Count];
                for (var i = 0; i < MapTextBoxes.Count; i++)
                {
                    var mapStr = MapTextBoxes[i].Text.Replace(";", Environment.NewLine);
                    if (string.IsNullOrWhiteSpace(mapStr))
                    {
                        continue;
                    }
                    var map = Parser.Parse(mapStr);
                    maps[i] = map;
                }
            }
            if (probabilities == null)
            {
                probabilities = new Dictionary<Coordinate, decimal>();
            }
            var bmp = new Bitmap(MainPictureBox.Width, MainPictureBox.Height);

            var textBrush = new SolidBrush(Color.FromArgb(255,255,255));
            var cellWidth = bmp.Width/maps[0].Height;
            var cellHeight = bmp.Height/maps[0].Width;
            if (cellHeight > cellWidth)
            {
                cellHeight = cellWidth;
            }
            if (cellWidth > cellHeight)
            {
                cellWidth = cellHeight;
            }

            RescaleTiles(cellHeight, cellWidth);

            var font = new Font(Font.FontFamily, 7, FontStyle.Bold);

            var borderIncrement = (cellWidth/2-5)/ maps.Length;

            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < maps[0].Width; i++)
                {
                    for (var j = 0; j < maps[0].Height; j++)
                    {
                        
                        graphics.FillRectangle(EmptyBrush, j*cellWidth, i*cellHeight, cellWidth, cellHeight);
                        var cell = maps[0].Cells[i, j];
                        DisplayCell(graphics, cell, j * cellWidth, i * cellHeight, cellWidth, cellHeight, textBrush);

                        var borderWidth = 0;
                        for (var k = 1; k < maps.Length; k++)
                        {
                            if (maps[k]?.Cells[i,j].State == CellState.Filled)
                            {
                                graphics.FillRectangle(Brushes[k], j * cellWidth + borderWidth + 1, i * cellHeight + borderWidth + 1, cellWidth - 2*borderWidth - 1, cellHeight - 2*borderWidth - 1);
                                borderWidth += borderIncrement;
                            }
                        }
                        var posStr = $"[{i};{j}]";
                        graphics.DrawString(posStr, font, textBrush, j*cellWidth , i*cellHeight);
                        decimal probability;
                        if(probabilities.TryGetValue(cell.Coordinate, out probability))
                        {
                            var probabilityStr = probability.ToString("##0.00%");
                            graphics.DrawString(probabilityStr, font, textBrush, j * cellWidth, i * cellHeight + cellHeight - 15);
                        }
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

            MainPictureBox.Image = bmp;
            MainPictureBox.Invalidate();
        }

        private Map GetMask(IDictionary<Coordinate, Verdict> verdicts, Verdict targetVerdict, int width, int height)
        {
            var map = new Map(width, height, true);
            foreach (var verdict in verdicts)
            {
                if (verdict.Value == targetVerdict)
                {
                    map.Cells[verdict.Key.X, verdict.Key.Y].State = CellState.Filled;
                }
            }
            return map;
        }

        private void SolveMapButton_Click(object sender, EventArgs e)
        {
            var solver = new BorderSeparationSolver();
            var map = Parser.Parse(Map0TextBox.Text);
            IDictionary<Coordinate, decimal> probabilities;
            var verdicts = solver.Solve(map, out probabilities);
            if (verdicts != null)
            {
                var maskHasMine = GetMask(verdicts, Verdict.HasMine, map.Width, map.Height);
                var maskDoesntHaveMine = GetMask(verdicts, Verdict.DoesntHaveMine, map.Width, map.Height);
                MapTextBoxes[1].Text = Visualizer.VisualizeToString(maskDoesntHaveMine);
                MapTextBoxes[2].Text = Visualizer.VisualizeToString(maskHasMine);
            }
            DisplayMaps(probabilities:probabilities);
        }
    }
}
