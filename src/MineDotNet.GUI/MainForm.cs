using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Windows.Forms;
using MineDotNet.AI;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game;

namespace MineDotNet.GUI
{
    public partial class MainForm : Form
    {
        private IList<TextBox> MapTextBoxes { get; }
        private ISolver Solver { get; }
        
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

            var aggregateSolver = new BorderSeparationSolver();
#if DEBUG
            aggregateSolver.Debug += AiOnDebug;
#endif
            Solver = aggregateSolver;

            Parser = new TextMapParser();
            Visualizer = new TextMapVisualizer();
            var allMaps = maps.ToList();
            MapCount = allMaps.Count > 3 ? allMaps.Count : 3;
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
                allMaps.Add(new Map(8,8,null,true));
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

        private static void AiOnDebug(string s)
        {
            Debug.Write(s);
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

        private Map GetMask(IDictionary<Coordinate, SolverResult> results, bool targetVerdict, int width, int height)
        {
            var map = new Map(width, height, null, true);
            foreach (var result in results)
            {
                if (result.Value.Verdict == targetVerdict)
                {
                    map[result.Key].State = CellState.Filled;
                }
            }
            return map;
        }

        private void SolveMapButton_Click(object sender, EventArgs e)
        {
            var map = Parser.Parse(Map0TextBox.Text);
            var results = AI.AI.Solve(map);
            DisplayResults(map, results);
        }

        private void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            if (results != null)
            {
                var maskHasMine = GetMask(results, true, map.Width, map.Height);
                var maskDoesntHaveMine = GetMask(results, false, map.Width, map.Height);
                MapTextBoxes[1].Text = Visualizer.VisualizeToString(maskDoesntHaveMine);
                MapTextBoxes[2].Text = Visualizer.VisualizeToString(maskHasMine);
            }
            var maps = GetMapsFromTextBoxes();
            Display.DisplayMaps(maps, results);
        }

        private void AutoPlayButton_Click(object sender, EventArgs e)
        {
            var random = new Random();
            var generator = new GameMapGenerator(random);
            var width = 32;
            var height = 32;
            var startingPos = new Coordinate(random.Next(0, width), random.Next(0, height));
            var density = MineDensityTrackBar.Value/(double) 100;
            var gameMap = generator.GenerateWithMineDensity(width, height, startingPos, density);
            while (true)
            {
                var regularMap = gameMap.ToRegularMap();
                MapTextBoxes[0].Text = Visualizer.VisualizeToString(regularMap);
                MapTextBoxes[1].Text = string.Empty;
                MapTextBoxes[2].Text = string.Empty;
                Display.DisplayMaps(new[] { regularMap });
                Application.DoEvents();
                var results = AI.AI.Solve(regularMap);
                if (results.Count == 0)
                {
                    MessageBox.Show("Done");
                    return;
                }
                foreach (var result in results)
                {
                    if (!result.Value.Verdict.HasValue)
                    {
                        continue;
                    }
                    if (result.Value.Verdict.Value)
                    {
                        gameMap[result.Key].Flag = CellFlag.HasMine;
                        gameMap.RemainingMineCount--;
                    }
                    else
                    {
                        if (gameMap[result.Key].HasMine)
                        {
                            regularMap[result.Key].Flag = CellFlag.NotSure;
                            Map0TextBox.Text = Visualizer.VisualizeToString(regularMap);
                            DisplayResults(regularMap, results);
                            MessageBox.Show("Boom " + result.Key);
                            return;
                        }
                        gameMap[result.Key].State = CellState.Empty;
                    }
                }
                DisplayResults(regularMap, results);
                Application.DoEvents();
                Thread.Sleep(100);
            }
        }

        private void MineDensityTrackBar_ValueChanged(object sender, EventArgs e)
        {
            MineDensityLabel.Text = $"Mine density: {MineDensityTrackBar.Value}%";
        }
    }
}
