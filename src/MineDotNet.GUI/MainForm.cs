using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MineDotNet.Common;

namespace MineDotNet.GUI
{
    public partial class MainForm : Form
    {
        private IList<TextBox> MapTextBoxes { get; }
        private IList<SolidBrush> Brushes { get;}
        private SolidBrush EmptyBrush { get; }
        private int MapCount { get; }

        public MainForm() : this(new Map[0])
        {
        }

        public MainForm(IList<Map> maps)
        {
            InitializeComponent();
            var allMaps = maps.ToList();
            MapCount = 2;
            MapTextBoxes = new TextBox[MapCount];
            MapTextBoxes[0] = Map0TextBox;

            var colors = new List<Color>
            {
                Color.FromArgb(0, 0, 0),
                Color.FromArgb(100, 0, 0),
                Color.FromArgb(0, 100, 0),
                Color.FromArgb(40, 70, 170),
                Color.FromArgb(100, 100, 0),
                Color.FromArgb(100, 0, 100),
                Color.FromArgb(0, 100, 100),
                Color.FromArgb(170, 70, 0),
                Color.FromArgb(0, 170, 100),
                Color.FromArgb(70, 30, 0),
                Color.FromArgb(180, 0, 100),
                Color.FromArgb(180, 150, 50),
                Color.FromArgb(120, 120, 120),
                Color.FromArgb(170, 170, 170),
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
            EmptyBrush = new SolidBrush(Color.FromArgb(80, 80, 80));


            var offset = 240;

            for (var i = 1; i < MapCount; i++)
            {
                var newTextBox = new TextBox();
                newTextBox.Parent = this;
                newTextBox.Location = new Point(Map0TextBox.Location.X, Map0TextBox.Location.Y + offset * i);
                newTextBox.Multiline = Map0TextBox.Multiline;
                newTextBox.Size = Map0TextBox.Size;
                newTextBox.Anchor = Map0TextBox.Anchor;

                var newLabel = new Label();
                newLabel.Parent = this;
                newLabel.Location = new Point(Map0Label.Location.X, Map0Label.Location.Y + offset * i);
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

            var visualizer = new TextMapVisualizer();
            for (var i = 0; i < allMaps.Count; i++)
            {
                if (i >= MapTextBoxes.Count)
                {
                    break;
                }
                var mapStr = visualizer.VisualizeToString(allMaps[i]);
                MapTextBoxes[i].Text = mapStr;
            }


            DisplayMaps(allMaps.ToArray());
        }

        private void ShowMapsButton_Click(object sender, EventArgs e)
        {
            var parser = new TextMapParser();
            var maps = new Map[MapTextBoxes.Count];
            for (var i = 0; i < MapTextBoxes.Count; i++)
            {
                var mapStr = MapTextBoxes[i].Text.Replace(";", Environment.NewLine);
                if (string.IsNullOrWhiteSpace(mapStr))
                {
                    continue;
                }
                var map = parser.Parse(mapStr);
                maps[i] = map;
            }

            DisplayMaps(maps);
        }

        private void DisplayCell(Graphics graphics, string str, int x, int y, int width, int height, Brush textBrush)
        {
            var font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            if (str != null)
            {
                graphics.DrawString(str, font, textBrush, x + width/2 - 12, y + height/2 - 7);
            }
        }

        private void DisplayMaps(params Map[] maps)
        {
            var visualizer = new TextMapVisualizer();
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
            var font = new Font(Font.FontFamily, 7, FontStyle.Bold);

            var borderIncrement = (cellWidth/2-5)/ maps.Length;

            using (var graphics = Graphics.FromImage(bmp))
            {
                for (var i = 0; i < maps[0].Width; i++)
                {
                    for (var j = 0; j < maps[0].Height; j++)
                    {
                        
                        graphics.FillRectangle(EmptyBrush, j*cellWidth, i*cellHeight, cellWidth, cellHeight);
                        var cellStr = visualizer.VisualizeCell(maps[0].Cells[i, j]);
                        DisplayCell(graphics, cellStr, j * cellWidth, i * cellHeight, cellWidth, cellHeight, textBrush);

                        var borderWidth = 0;
                        for (var k = 1; k < maps.Length; k++)
                        {
                            if (maps[k]?.Cells[i,j].State == CellState.Filled)
                            {
                                graphics.FillRectangle(Brushes[k], j * cellWidth + borderWidth + 1, i * cellHeight + borderWidth + 1, cellWidth - 2*borderWidth - 1, cellHeight - 2*borderWidth - 1);
                                borderWidth += borderIncrement;
                            }
                        }
                        var pos = $"[{i};{j}]";
                        graphics.DrawString(pos, font, textBrush, j*cellWidth , i*cellHeight);
                    }
                }

                for (var i = 0; i <= maps[0].Height; i++)
                {
                    graphics.DrawLine(Pens.Black, cellWidth*i, 0, cellWidth*i, cellHeight*maps[0].Width);
                }
                for (var i = 0; i <= maps[0].Width; i++)
                {
                    graphics.DrawLine(Pens.Black, 0, cellHeight * i, cellHeight * maps[0].Height, cellHeight * i);
                }
            }

            MainPictureBox.Image = bmp;
            MainPictureBox.Invalidate();
        }
    }
}
