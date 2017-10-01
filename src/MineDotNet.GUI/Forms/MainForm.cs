using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MineDotNet.AI.Solvers;
using MineDotNet.Common;
using MineDotNet.Game;
using MineDotNet.GUI.Models;
using MineDotNet.GUI.Services;

namespace MineDotNet.GUI.Forms
{
    public partial class MainForm : Form
    {
        private IList<TextBox> MapTextBoxes { get; }
        private ISolver Solver { get; }
        
        private TextMapParser Parser { get; }
        private TextMapVisualizer Visualizer { get; }
        private int MapCount { get; }

        private DisplayService Display { get; set; }

        private GameMapGenerator GameMapGenerator { get; set; }
        private GameEngine CurrentManualGameEngine { get; set; }
        private double MineDensity => MineDensityTrackBar.Value/(double) 100;
        private int Width => (int)WidthNumericUpDown.Value;
        private int Height => (int)HeightNumericUpDown.Value;
        public MainForm() : this(new Map[0])
        {
        }

        public MainForm(IList<Map> maps)
        {
            InitializeComponent();

            var aggregateSolver = new BorderSeparationSolver();
            Solver = aggregateSolver;

            Parser = new TextMapParser();
            Visualizer = new TextMapVisualizer();
            var allMaps = maps.ToList();
            MapCount = allMaps.Count > 3 ? allMaps.Count : 3;
            MapTextBoxes = new TextBox[MapCount];
            MapTextBoxes[0] = Map0TextBox;

            Display = new DisplayService(MainPictureBox, MapCount, Visualizer);
            Display.CellClick += DisplayOnCellClick;
            var random = new Random();
            GameMapGenerator = new GameMapGenerator(random);
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
            var maskMaps = allMaps.Skip(1).Select(MaskMap.FromMap).ToList();
            Display.DisplayMap(allMaps[0], maskMaps);
        }

        private void DisplayOnCellClick(object sender, CellClickEventArgs args)
        {
            if (CurrentManualGameEngine == null)
            {
                return;
            }
            if ((args.Buttons & MouseButtons.Right) != 0)
            {
                if (!CurrentManualGameEngine.GameStarted)
                {
                    return;
                }
                CurrentManualGameEngine.ToggleFlag(args.Coordinate);
            }
            else if((args.Buttons & MouseButtons.Left) != 0)
            {
                if (!CurrentManualGameEngine.GameStarted)
                {
                    CurrentManualGameEngine.StartWithMineDensity(GameMapGenerator, Width, Height, args.Coordinate, true, MineDensity);
                }
                else
                {
                    var success = CurrentManualGameEngine.OpenCell(args.Coordinate);
                    if (!success)
                    {
                        MessageBox.Show("Boom " + args.Coordinate);
                        return;
                    }
                }
            }
            var regularMap = CurrentManualGameEngine.GameMap.ToRegularMap();
            MapTextBoxes[0].Text = Visualizer.VisualizeToString(regularMap);
            var maskMaps = GetMaskMaps();
            Display.DisplayMap(regularMap, maskMaps);
        }

        private void ShowMapsButton_Click(object sender, EventArgs e)
        {
            GetAndDisplayMap();
            //var editor = new SolverSettingsEditorForm();
            //editor.ShowDialog();
        }

        private void GetAndDisplayMap(IDictionary<Coordinate, SolverResult> results = null)
        {
            var map = GetMap();
            var maskMaps = GetMaskMaps();
            Display.DisplayMap(map, maskMaps, results);
        }

        private Map GetMap()
        {
            var maps = new Map[MapTextBoxes.Count];
            var mapStr = MapTextBoxes[0].Text.Replace(";", Environment.NewLine);
            if(string.IsNullOrWhiteSpace(mapStr))
            {
                // TODO: Handle this
            }
            var map = Parser.Parse(mapStr);
            return map;
        }

        private IList<MaskMap> GetMaskMaps()
        {
            var maps = new List<MaskMap>();
            for (var i = 1; i < MapTextBoxes.Count; i++)
            {
                var mapStr = MapTextBoxes[i].Text.Replace(";", Environment.NewLine);
                if (string.IsNullOrWhiteSpace(mapStr))
                {
                    continue;
                }
                var map = Parser.Parse(mapStr);
                var maskMap = MaskMap.FromMap(map);
                maps.Add(maskMap);
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
            GetAndDisplayMap(results);
        }

        private void AutoPlayButton_Click(object sender, EventArgs e)
        {
            var engine = new GameEngine();
            engine.StartWithMineDensity(GameMapGenerator, Width, Height, new Coordinate(Width/2, Height/2), true, MineDensity);
            while (true)
            {
                var regularMap = engine.GameMap.ToRegularMap();
                MapTextBoxes[0].Text = Visualizer.VisualizeToString(regularMap);
                MapTextBoxes[1].Text = string.Empty;
                MapTextBoxes[2].Text = string.Empty;
                //Display.DisplayMaps(new[] { regularMap });
                Application.DoEvents();
                var results = AI.AI.Solve(regularMap);
                if (results.Count == 0)
                {
                    MessageBox.Show("Done");
                    return;
                }
                foreach (var result in results)
                {
                    switch (result.Value.Verdict)
                    {
                        case true:
                            engine.SetFlag(result.Key, CellFlag.HasMine);
                            break;
                        case false:
                            var succesfullyOpened = engine.OpenCell(result.Key);
                            if (!succesfullyOpened)
                            {
                                regularMap[result.Key].Flag = CellFlag.NotSure;
                                Map0TextBox.Text = Visualizer.VisualizeToString(regularMap);
                                DisplayResults(regularMap, results);
                                MessageBox.Show("Boom " + result.Key);
                                return;
                            }
                            break;
                        case null:
                            break;
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

        private void ManualPlayButton_Click(object sender, EventArgs e)
        {
            CurrentManualGameEngine = new GameEngine();
            var emptyMap = new Map(Width, Height, null, true, CellState.Filled);
            Display.DisplayMap(emptyMap, new List<MaskMap>());
        }
    }
}
