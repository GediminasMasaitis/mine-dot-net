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

        
        private TextMapParser Parser { get; }
        private TextMapVisualizer Visualizer { get; }
        private int MapCount { get; }

        private DisplayService Display { get; set; }

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

            Display = new DisplayService(MainPictureBox, MapCount, Visualizer);
            

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
                newLabel.ForeColor = Display.Brushes[i].Color;
                newLabel.Anchor = Map0Label.Anchor;

                MapTextBoxes[i] = newTextBox;
            }

            if (allMaps.Count == 0)
            {
                allMaps.Add(new Map(8,8,true));
            }


            for (var i = 0; i < MapTextBoxes.Count; i++)
            {
                MapTextBoxes[i].ForeColor = Display.Brushes[i].Color;
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
            
            Display.TryLoadAssets();
            Display.DisplayMaps(allMaps.ToArray());
        }

        

        private void ShowMapsButton_Click(object sender, EventArgs e)
        {
            var maps = GetMapsFromTextBoxes();
            Display.DisplayMaps(maps);
        }

        private Map[] GetMapsFromTextBoxes()
        {
            var maps = new Map[MapTextBoxes.Count];
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
            return maps;
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
            var maps = GetMapsFromTextBoxes();
            Display.DisplayMaps(maps, probabilities);
        }
    }
}
