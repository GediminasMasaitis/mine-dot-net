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
    internal partial class MainForm : Form
    {
        
        private ISolver Solver { get; }
        private readonly IDisplayService _display;
        

        private GameMapGenerator GameMapGenerator { get; set; }
        private GameEngine CurrentManualGameEngine { get; set; }
        private double MineDensity => MineDensityTrackBar.Value/(double) 100;
        private int Width => (int)WidthNumericUpDown.Value;
        private int Height => (int)HeightNumericUpDown.Value;

        public MainForm()
        {
            InitializeComponent();

            _display = IOCC.GetService<IDisplayService>();
            _display.SetTarget(MainPictureBox);
            _display.CellClick += DisplayOnCellClick;

            var aggregateSolver = new BorderSeparationSolver();
            Solver = aggregateSolver;

            var random = new Random();
            GameMapGenerator = new GameMapGenerator(random);

            var defaultMap = new Map(8, 8, null, true);
            SetMapAndMasks(defaultMap, null);
        }

        public void SetMapAndMasks(Map map, IList<Mask> masks)
        {
            if (map != null)
            {
                MapTextVisualizers.SetMap(map);
            }

            MapTextVisualizers.SetMasks(masks);
            _display.DisplayMap(map, masks);
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
            MapTextVisualizers.SetMap(regularMap);
            var maskMaps = MapTextVisualizers.GetMasks();
            _display.DisplayMap(regularMap, maskMaps);
        }

        private void ShowMapsButton_Click(object sender, EventArgs e)
        {
            GetAndDisplayMap();
            //var editor = new SolverSettingsEditorForm();
            //editor.ShowDialog();
        }

        private void GetAndDisplayMap(IDictionary<Coordinate, SolverResult> results = null)
        {
            var map = MapTextVisualizers.GetMap();
            var maskMaps = MapTextVisualizers.GetMasks();
            _display.DisplayMap(map, maskMaps, results);
        }
        


        private void SolveMapButton_Click(object sender, EventArgs e)
        {
            var map = MapTextVisualizers.GetMap();
            var results = AI.AI.Solve(map);
            DisplayResults(map, results);
        }

        private void DisplayResults(IMap map, IDictionary<Coordinate, SolverResult> results)
        {
            MapTextVisualizers.DisplayResults(map, results);
            GetAndDisplayMap(results);
        }

        private void AutoPlayButton_Click(object sender, EventArgs e)
        {
            var engine = new GameEngine();
            engine.StartWithMineDensity(GameMapGenerator, Width, Height, new Coordinate(Width/2, Height/2), true, MineDensity);
            while (true)
            {
                var regularMap = engine.GameMap.ToRegularMap();
                MapTextVisualizers.SetMap(regularMap);
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
                                MapTextVisualizers.SetMap(regularMap);
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
            _display.DisplayMap(emptyMap, new List<Mask>());
        }
    }
}
